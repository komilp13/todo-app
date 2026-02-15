using System.Text.Json;

namespace TodoApp.Api.Utilities;

/// <summary>
/// Utility class for extracting values from JSON deserialization results.
/// Handles both direct type conversions and JsonElement extractions.
/// </summary>
public static class JsonValueExtractor
{
    /// <summary>
    /// Extracts a string value from an object that may be a string or JsonElement.
    /// This is useful when deserializing JSON to Dictionary&lt;string, object?&gt; where
    /// ASP.NET Core returns JsonElement instances instead of direct types.
    /// </summary>
    /// <param name="value">The value to extract, may be null, string, or JsonElement.</param>
    /// <returns>The extracted string value, or null if the value is null or JsonElement.Null.</returns>
    public static string? GetStringValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is string str)
        {
            return str;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Null => null,
                JsonValueKind.Number => element.ToString(),
                _ => null
            };
        }

        return value.ToString();
    }
}
