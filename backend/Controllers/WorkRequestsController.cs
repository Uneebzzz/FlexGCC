using Microsoft.AspNetCore.Mvc;
using WorkRequestTracker.Api.Models;
using WorkRequestTracker.Api.Services;

namespace WorkRequestTracker.Api.Controllers;

[ApiController]
[Route("api/work-requests")]
public class WorkRequestsController(WorkRequestService service, ILogger<WorkRequestsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<WorkRequestSummary>>> List(
        [FromQuery] string? status, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            return Ok(await service.ListAsync(status, search, page, pageSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing work requests");
            throw;
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkRequestDetail>> Get(int id)
    {
        try
        {
            return Ok(await service.GetAsync(id));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting work request {Id}", id);
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult<WorkRequestDetail>> Create(CreateWorkRequestRequest request)
    {
        try
        {
            var created = await service.CreateAsync(request);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating work request");
            throw;
        }
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<WorkRequestDetail>> UpdateStatus(int id, UpdateStatusRequest request)
    {
        try
        {
            return Ok(await service.UpdateStatusAsync(id, request));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating status for work request {Id}", id);
            throw;
        }
    }

    [HttpPost("{id:int}/notes")]
    public async Task<ActionResult<NoteResponse>> AddNote(int id, AddNoteRequest request)
    {
        try
        {
            return StatusCode(StatusCodes.Status201Created, await service.AddNoteAsync(id, request));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding note to work request {Id}", id);
            throw;
        }
    }
}
