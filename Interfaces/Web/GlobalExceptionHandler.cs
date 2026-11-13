using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RealTimeApps.Interfaces.Web;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger; 

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct
    )
    {
        (int statusCode, string title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"), 
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"), 
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request"), 
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error") 
        };

        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception for {Path}", httpContext.Request.Path);
        else 
            _logger.LogWarning("Request to {Path} failed: {Message}", 
                httpContext.Request.Path, exception.Message);
        
        ProblemDetails problem = new()
        {
            Status = statusCode,
            Title = title, 
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occured"
                : exception.Message, 
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            problem, 
            options: null, 
            contentType: "application/problem+json",
            cancellationToken: ct
        );

        return true;
    }
}
