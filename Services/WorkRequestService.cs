using Microsoft.EntityFrameworkCore;
using WorkRequestTracker.Api.Data;
using Microsoft.Extensions.Logging;
using WorkRequestTracker.Api.Errors;
using WorkRequestTracker.Api.Models;

namespace WorkRequestTracker.Api.Services;

public class WorkRequestService(AppDbContext db, ILogger<WorkRequestService> logger)
{
    public async Task<PagedResult<WorkRequestSummary>> ListAsync(string? status, string? search, int page, int pageSize)
    {
        try
        {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.WorkRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsed = ParseEnum<WorkRequestStatus>(status, "status");
            query = query.Where(w => w.Status == parsed);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(w => EF.Functions.Like(w.Title, $"%{term}%")
                                  || EF.Functions.Like(w.ClientName, $"%{term}%"));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(w => new WorkRequestSummary(
                w.Id, w.Title, w.ClientName, w.Priority.ToString(), w.Status.ToString(),
                w.DueDate, w.CreatedAt, w.UpdatedAt, w.Notes.Count))
            .ToListAsync();

        return new PagedResult<WorkRequestSummary>(items, page, pageSize, total);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing work requests (status={Status}, search={Search}, page={Page}, pageSize={PageSize})", status, search, page, pageSize);
            throw;
        }
    }

    public async Task<WorkRequestDetail> GetAsync(int id)
    {
        try
        {
            var w = await db.WorkRequests.AsNoTracking().Include(x => x.Notes)
                        .FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new NotFoundException($"Work request {id} was not found.");
            return ToDetail(w);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting work request {Id}", id);
            throw;
        }
    }

    public async Task<WorkRequestDetail> CreateAsync(CreateWorkRequestRequest r)
    {
        try
        {
            var errors = new Dictionary<string, string[]>();
            var priority = TryParse<Priority>(r.Priority, "priority", errors);
            var status = TryParse<WorkRequestStatus>(r.Status, "status", errors);
            if (string.IsNullOrWhiteSpace(r.Title)) errors["title"] = ["Title is required."];
            if (string.IsNullOrWhiteSpace(r.ClientName)) errors["clientName"] = ["Client name is required."];
            if (errors.Count > 0) throw new ValidationFailedException(errors);

            var now = DateTime.UtcNow;
            var entity = new WorkRequest
            {
                Title = r.Title!.Trim(),
                ClientName = r.ClientName!.Trim(),
                Description = r.Description?.Trim(),
                Priority = priority!.Value,
                Status = status!.Value,
                DueDate = r.DueDate!.Value.ToUniversalTime(),
                CreatedAt = now,
                UpdatedAt = now
            };
            db.WorkRequests.Add(entity);
            await db.SaveChangesAsync();
            return ToDetail(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating work request");
            throw;
        }
    }

    public async Task<WorkRequestDetail> UpdateStatusAsync(int id, UpdateStatusRequest r)
    {
        try
        {
            var errors = new Dictionary<string, string[]>();
            var status = TryParse<WorkRequestStatus>(r.Status, "status", errors);
            if (errors.Count > 0) throw new ValidationFailedException(errors);

            var w = await db.WorkRequests.Include(x => x.Notes).FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new NotFoundException($"Work request {id} was not found.");

            w.Status = status!.Value;
            w.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return ToDetail(w);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating status for work request {Id}", id);
            throw;
        }
    }

    public async Task<NoteResponse> AddNoteAsync(int id, AddNoteRequest r)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(r.Text))
                throw new ValidationFailedException(new() { ["text"] = ["Note text is required."] });

            var w = await db.WorkRequests.FirstOrDefaultAsync(x => x.Id == id)
                    ?? throw new NotFoundException($"Work request {id} was not found.");

            var note = new WorkRequestNote
            {
                WorkRequestId = id,
                Text = r.Text.Trim(),
                Author = r.Author?.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            db.Notes.Add(note);
            w.UpdatedAt = note.CreatedAt;          // a note counts as activity on the request
            await db.SaveChangesAsync();
            return new NoteResponse(note.Id, note.Text, note.Author, note.CreatedAt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding note to work request {Id}", id);
            throw;
        }
    }

    // ---- helpers ----
    private static T ParseEnum<T>(string value, string field) where T : struct, Enum
    {
        // Enum.TryParse accepts numbers like "7"; IsDefined rejects them.
        if (Enum.TryParse<T>(value, ignoreCase: true, out var result) && Enum.IsDefined(result)
            && !int.TryParse(value, out _))
            return result;
        throw new ValidationFailedException(new() { [field] = [EnumMessage<T>(field)] });
    }

    private static T? TryParse<T>(string? value, string field, Dictionary<string, string[]> errors) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) { errors[field] = [$"{char.ToUpper(field[0]) + field[1..]} is required."]; return null; }
        if (Enum.TryParse<T>(value, true, out var result) && Enum.IsDefined(result) && !int.TryParse(value, out _))
            return result;
        errors[field] = [EnumMessage<T>(field)];
        return null;
    }

    private static string EnumMessage<T>(string field) where T : struct, Enum =>
        $"Invalid {field}. Allowed values: {string.Join(", ", Enum.GetNames<T>())}.";

    private static WorkRequestDetail ToDetail(WorkRequest w) => new(
        w.Id, w.Title, w.ClientName, w.Description, w.Priority.ToString(), w.Status.ToString(),
        w.DueDate, w.CreatedAt, w.UpdatedAt,
        w.Notes.OrderByDescending(n => n.CreatedAt)
               .Select(n => new NoteResponse(n.Id, n.Text, n.Author, n.CreatedAt)).ToList());
}
