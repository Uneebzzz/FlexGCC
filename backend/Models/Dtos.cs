using System.ComponentModel.DataAnnotations;

namespace WorkRequestTracker.Api.Models;

// Enums arrive as strings so we can return a friendly 400 for bad values
// instead of a generic JSON-binding failure.
public class CreateWorkRequestRequest
{
    [Required, StringLength(200)] public string? Title { get; set; }
    [Required, StringLength(200)] public string? ClientName { get; set; }
    [StringLength(4000)] public string? Description { get; set; }
    [Required] public string? Priority { get; set; }
    [Required] public string? Status { get; set; }
    [Required] public DateTime? DueDate { get; set; }
}

public class UpdateStatusRequest
{
    [Required] public string? Status { get; set; }
}

public class AddNoteRequest
{
    [Required, StringLength(2000)] public string? Text { get; set; }
    [StringLength(100)] public string? Author { get; set; }
}

public record NoteResponse(int Id, string Text, string? Author, DateTime CreatedAt);

public record WorkRequestSummary(
    int Id, string Title, string ClientName, string Priority, string Status,
    DateTime DueDate, DateTime CreatedAt, DateTime UpdatedAt, int NoteCount);

public record WorkRequestDetail(
    int Id, string Title, string ClientName, string? Description, string Priority,
    string Status, DateTime DueDate, DateTime CreatedAt, DateTime UpdatedAt,
    IReadOnlyList<NoteResponse> Notes);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
