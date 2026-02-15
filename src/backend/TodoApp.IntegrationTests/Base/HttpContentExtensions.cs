using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TodoApp.IntegrationTests.Base;

/// <summary>
/// Extension methods for HttpContent to support JSON deserialization with enum converters.
/// </summary>
public static class HttpContentExtensions
{
    /// <summary>
    /// Gets the JSON serializer options configured with enum converters (matches server-side configuration).
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Reads the HTTP content as JSON with proper enum deserialization support.
    /// Matches the server-side JSON configuration to ensure consistent serialization.
    /// </summary>
    public static async Task<T?> ReadFromJsonWithEnumSupportAsync<T>(this HttpContent content, CancellationToken cancellationToken = default)
    {
        using var stream = await content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }
}
