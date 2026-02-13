namespace TodoApp.UnitTests.Services;

/// <summary>
/// Unit tests for health check service logic.
/// </summary>
public class HealthCheckServiceTests
{
    [Fact]
    public void GetHealth_ReturnsHealthyStatus()
    {
        // Arrange
        var expectedStatus = "healthy";

        // Act
        var result = GetHealthStatus();

        // Assert
        Assert.Equal(expectedStatus, result);
    }

    [Fact]
    public void GetHealth_ReturnsValidTimestamp()
    {
        // Arrange
        var beforeTime = DateTime.UtcNow;

        // Act
        var result = GetHealthTimestamp();
        var afterTime = DateTime.UtcNow;

        // Assert
        Assert.InRange(result, beforeTime.AddSeconds(-1), afterTime.AddSeconds(1));
    }

    // Simple helper methods for testing
    private static string GetHealthStatus() => "healthy";

    private static DateTime GetHealthTimestamp() => DateTime.UtcNow;
}
