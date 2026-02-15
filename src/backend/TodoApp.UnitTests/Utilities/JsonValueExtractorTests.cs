using System.Text.Json;
using TodoApp.Api.Utilities;
using Xunit;

namespace TodoApp.UnitTests.Utilities;

/// <summary>
/// Unit tests for JsonValueExtractor utility class.
/// Tests the extraction of string values from various object types,
/// particularly JsonElement which is returned by ASP.NET Core when
/// deserializing JSON to Dictionary&lt;string, object?&gt;.
/// </summary>
public class JsonValueExtractorTests
{
    [Fact]
    public void GetStringValue_WithNullValue_ReturnsNull()
    {
        // Arrange
        object? value = null;

        // Act
        var result = JsonValueExtractor.GetStringValue(value);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetStringValue_WithStringValue_ReturnsString()
    {
        // Arrange
        object value = "test string";

        // Act
        var result = JsonValueExtractor.GetStringValue(value);

        // Assert
        Assert.Equal("test string", result);
    }

    [Fact]
    public void GetStringValue_WithJsonElementString_ReturnsString()
    {
        // Arrange
        var json = "\"test value\"";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

        // Act
        var result = JsonValueExtractor.GetStringValue(jsonElement);

        // Assert
        Assert.Equal("test value", result);
    }

    [Fact]
    public void GetStringValue_WithJsonElementNull_ReturnsNull()
    {
        // Arrange
        var json = "null";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

        // Act
        var result = JsonValueExtractor.GetStringValue(jsonElement);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetStringValue_WithJsonElementNumber_ReturnsNumberAsString()
    {
        // Arrange
        var json = "42";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

        // Act
        var result = JsonValueExtractor.GetStringValue(jsonElement);

        // Assert
        Assert.Equal("42", result);
    }

    [Fact]
    public void GetStringValue_WithJsonElementBoolean_ReturnsNull()
    {
        // Arrange
        var json = "true";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

        // Act
        var result = JsonValueExtractor.GetStringValue(jsonElement);

        // Assert
        // Boolean values should not be converted to strings
        Assert.Null(result);
    }

    [Fact]
    public void GetStringValue_WithJsonElementObject_ReturnsNull()
    {
        // Arrange
        var json = "{\"key\": \"value\"}";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

        // Act
        var result = JsonValueExtractor.GetStringValue(jsonElement);

        // Assert
        // Object values should not be converted to strings
        Assert.Null(result);
    }

    [Fact]
    public void GetStringValue_WithJsonElementArray_ReturnsNull()
    {
        // Arrange
        var json = "[1, 2, 3]";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);

        // Act
        var result = JsonValueExtractor.GetStringValue(jsonElement);

        // Assert
        // Array values should not be converted to strings
        Assert.Null(result);
    }

    [Fact]
    public void GetStringValue_WithIntegerValue_ReturnsStringRepresentation()
    {
        // Arrange
        object value = 123;

        // Act
        var result = JsonValueExtractor.GetStringValue(value);

        // Assert
        Assert.Equal("123", result);
    }

    [Fact]
    public void GetStringValue_WithEnumValue_ReturnsEnumName()
    {
        // Arrange
        object value = DayOfWeek.Monday;

        // Act
        var result = JsonValueExtractor.GetStringValue(value);

        // Assert
        Assert.Equal("Monday", result);
    }

    [Fact]
    public void GetStringValue_SimulatesDictionaryDeserialization()
    {
        // Arrange - Simulate what ASP.NET Core does when deserializing JSON to Dictionary<string, object?>
        var json = "{\"systemList\": \"Next\", \"priority\": \"P1\", \"count\": 42, \"flag\": true, \"empty\": null}";
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

        Assert.NotNull(dict);

        // Act & Assert - Extract string values
        var systemList = JsonValueExtractor.GetStringValue(dict["systemList"]);
        Assert.Equal("Next", systemList);

        var priority = JsonValueExtractor.GetStringValue(dict["priority"]);
        Assert.Equal("P1", priority);

        var count = JsonValueExtractor.GetStringValue(dict["count"]);
        Assert.Equal("42", count);

        var flag = JsonValueExtractor.GetStringValue(dict["flag"]);
        Assert.Null(flag); // Booleans should return null

        var empty = JsonValueExtractor.GetStringValue(dict["empty"]);
        Assert.Null(empty);
    }
}
