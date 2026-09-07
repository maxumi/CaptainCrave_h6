using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Middleware;

// Sikkerhedsnet for enhver fejl, en controller ikke selv har fået fanget.
// Oversætter fejltyper til de samme HTTP-statuskoder, controllerne ellers ville
// have brugt, og sørger for at klienten aldrig ser en rå stack trace.
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Fanger en ufanget fejl, så den ikke vælter hele serveren. Fejlen bliver logget, og
    /// klienten får et pænt JSON-svar (ProblemDetails) tilbage i stedet for en rå fejlbesked.
    /// </summary>
    /// <returns>True, for at fortælle ASP.NET Core at fejlen er blevet håndteret færdig.</returns>
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

        // Uventede (500) fejl logges med fuld detalje; alt andet er et kendt/forventet udfald.
        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning(exception, "{ExceptionType} on {Method} {Path}: {Message}", exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        httpContext.Response.StatusCode = statusCode;

        // Den rigtige fejlbesked må aldrig lække ved 500'ere, kun ved de kendte, forventede fejltyper ovenfor.
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
