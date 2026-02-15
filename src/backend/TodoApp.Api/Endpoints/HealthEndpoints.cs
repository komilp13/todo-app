namespace TodoApp.Api.Endpoints;

using System.Text.Json;
using TodoApp.Api.Extensions;

/// <summary>
/// Health check endpoints for monitoring application health.
/// </summary>
public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/health", (HttpContext context) => GetHealth(context))
            .WithName("GetHealth")
            .WithOpenApi();
    }

    private static async Task GetHealth(HttpContext context)
    {
        var response = new HealthResponse
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow
        };

        // Manually serialize and write to avoid .NET 9.0 PipeWriter issue in test environment
        var json = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Health check response model.
    /// </summary>
    public record HealthResponse
    {
        public required string Status { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}
