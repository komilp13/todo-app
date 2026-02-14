namespace TodoApp.Application.Features.Auth.Login;

/// <summary>
/// Command to authenticate a user with email and password.
/// </summary>
public class LoginCommand
{
    /// <summary>
    /// User's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password (will be verified against stored hash).
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
