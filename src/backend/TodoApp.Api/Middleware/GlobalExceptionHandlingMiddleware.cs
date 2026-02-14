namespace TodoApp.Api.Middleware;

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

        var errorResponse = new
        {
            statusCode = StatusCodes.Status500InternalServerError,
            message = "An internal server error occurred. Please try again later.",
            correlationId = correlationId,
            timestamp = DateTime.UtcNow
        };

        response.StatusCode = StatusCodes.Status500InternalServerError;

        // Use WriteAsJsonAsync safely without awaiting inside the return statement
        try
        {
            await response.WriteAsJsonAsync(errorResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write error response");
            // Fallback: write plain text response
            response.ContentType = "text/plain";
            await response.WriteAsync("An internal server error occurred.");
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
