using TodoApp.IntegrationTests.Base;
using System.Net;
using System.Text.Json;

namespace TodoApp.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for health check endpoint.
/// </summary>
public class HealthEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task GetHealth_ReturnsOkStatusCode()
    {
        // Act
        var response = await Client!.GetAsync("/api/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        // Act
        var response = await Client!.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        var status = root.GetProperty("status").GetString();

        // Assert
        Assert.Equal("healthy", status);
    }

    [Fact]
    public async Task GetHealth_ReturnsValidTimestamp()
    {
        // Act
        var response = await Client!.GetAsync("/api/health");
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        var timestamp = root.GetProperty("timestamp").GetDateTime();

        // Assert
        var now = DateTime.UtcNow;
        Assert.InRange(timestamp, now.AddMinutes(-1), now.AddMinutes(1));
    }
}
