namespace TodoApp.Application.Configuration;

/// <summary>
/// JWT configuration settings from appsettings.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key for signing tokens (min 256 bits / 32 bytes)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer claim
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience claim
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes (default: 1440 = 24 hours)
    /// </summary>
    public int ExpirationMinutes { get; set; } = 1440;
}
