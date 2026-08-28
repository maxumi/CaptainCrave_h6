using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Middleware;

// Safety net for any exception a controller didn't already catch itself. Keeps the mapping
// from exception type to HTTP status consistent with what controllers already do by hand,
// and makes sure nothing ever reaches the client as a raw stack trace.
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status400BadRequest, "Resource not found."),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Access denied."),
            DbUpdateException => (StatusCodes.Status409Conflict, "The request conflicts with existing data."),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "The request could not be processed."),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        // Unexpected (500) exceptions are logged with full detail; anything else is a known/expected outcome.
        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning(exception, "{ExceptionType} on {Method} {Path}: {Message}", exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        httpContext.Response.StatusCode = statusCode;

        // Never leak the real exception message for 500s — only for the known, expected exception types above.
        var detail = statusCode == StatusCodes.Status500InternalServerError ? "Please try again later." : exception.Message;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;
    }
}
