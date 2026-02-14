namespace TodoApp.Application.Features.Auth.Register;

/// <summary>
/// Response returned on successful user registration, containing JWT token and user profile.
/// </summary>
public class RegisterResponse
{
    /// <summary>
    /// JWT token for immediate authentication.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Newly created user's profile information.
    /// </summary>
    public UserDto User { get; set; } = null!;
}

/// <summary>
/// User profile information returned in registration response.
/// </summary>
public class UserDto
{
    /// <summary>
    /// User's unique identifier (GUID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
