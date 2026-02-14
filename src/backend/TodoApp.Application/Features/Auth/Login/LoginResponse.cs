using TodoApp.Application.Features.Auth.Register;

namespace TodoApp.Application.Features.Auth.Login;

/// <summary>
/// Response returned on successful user login, containing JWT token and user profile.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT token for authentication.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Authenticated user's profile information.
    /// </summary>
    public UserDto User { get; set; } = null!;
}
