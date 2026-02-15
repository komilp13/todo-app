namespace TodoApp.Api.Middleware;

using System.Text.Json;

/// <summary>
/// Global exception handling middleware that catches unhandled exceptions and returns standardized error responses.
/// </summary>
public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception, logger);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger logger)
    {
        logger.LogError(exception, "An unhandled exception occurred");

        var correlationId = context.TraceIdentifier;
        var response = context.Response;
        response.ContentType = "application/json";
        response.StatusCode = StatusCodes.Status500InternalServerError;

        var errorResponse = new
        {
            statusCode = StatusCodes.Status500InternalServerError,
            message = "An internal server error occurred. Please try again later.",
            correlationId = correlationId,
            timestamp = DateTime.UtcNow
        };

        // Use manual JSON serialization to avoid WriteAsJsonAsync issues in .NET 9.0 test environment
        try
        {
            var json = JsonSerializer.Serialize(errorResponse);
            await response.WriteAsync(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write error response as JSON");
            // Fallback: write plain text response
            try
            {
                response.ContentType = "text/plain";
                await response.WriteAsync("An internal server error occurred. Please try again later.");
            }
            catch
            {
                // Silently ignore if even plain text write fails
            }
        }
    }
}

/// <summary>
/// Extension methods for registering the global exception handling middleware.
/// </summary>
public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
