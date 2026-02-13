namespace TodoApp.Api.Endpoints;

/// <summary>
/// Health check endpoints for monitoring application health.
/// </summary>
public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/health", GetHealth)
            .WithName("GetHealth")
            .WithOpenApi()
            .Produces<HealthResponse>(StatusCodes.Status200OK);
    }

    private static IResult GetHealth()
    {
        var response = new HealthResponse
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow
        };

        return Results.Ok(response);
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
