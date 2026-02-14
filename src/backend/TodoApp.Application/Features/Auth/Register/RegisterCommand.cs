namespace TodoApp.Application.Features.Auth.Register;

/// <summary>
/// Command to register a new user with email, password, and display name.
/// </summary>
public class RegisterCommand
{
    /// <summary>
    /// User's email address (unique, case-insensitive).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password (will be hashed before storage).
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
