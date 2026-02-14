namespace TodoApp.Application.Features.Auth.CurrentUser;

/// <summary>
/// Response containing the current user's profile information.
/// </summary>
public class CurrentUserResponse
{
    /// <summary>
    /// User's unique identifier.
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

    /// <summary>
    /// Timestamp when user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
