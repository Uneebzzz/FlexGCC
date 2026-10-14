using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WorkRequestTracker.Api.Errors;

// One error shape for the whole API.
public record ApiError(string Code, string Message, Dictionary<string, string[]>? Details = null, string? TraceId = null);

public class NotFoundException(string message) : Exception(message);

public class ValidationFailedException(Dictionary<string, string[]> errors)
    : Exception("One or more fields are invalid.")
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, body) = ex switch
        {
            ValidationFailedException v => (400, new ApiError("validation_failed", v.Message, v.Errors, ctx.TraceIdentifier)),
            NotFoundException n         => (404, new ApiError("not_found", n.Message, null, ctx.TraceIdentifier)),
            _                           => (500, new ApiError("server_error", "An unexpected error occurred.", null, ctx.TraceIdentifier))
        };

        if (status == 500) logger.LogError(ex, "Unhandled exception"); // log details, never leak them

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(body, ct);
        return true;
    }
}

public static class ModelStateErrors
{
    // Turns [Required]/[StringLength] failures into the same ApiError shape.
    public static IActionResult Build(ActionContext ctx)
    {
        var details = ctx.ModelState
            .Where(e => e.Value!.Errors.Count > 0)
            .ToDictionary(
                e => char.ToLowerInvariant(e.Key[0]) + e.Key[1..],
                e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new ApiError(
            "validation_failed", "One or more fields are invalid.", details, ctx.HttpContext.TraceIdentifier));
    }
}
