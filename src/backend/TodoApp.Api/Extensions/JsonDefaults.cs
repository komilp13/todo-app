using System.Text.Json;

namespace TodoApp.Api.Extensions;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
