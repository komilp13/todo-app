using System.Text.Json;
using System.Text.Json.Serialization;

namespace TodoApp.IntegrationTests.Base;

/// <summary>
/// Helper for JSON deserialization in tests with proper enum converter support.
/// </summary>
public static class TestJsonHelper
{
    /// <summary>
    /// JSON serializer options configured with enum converters (matches server-side configuration).
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
