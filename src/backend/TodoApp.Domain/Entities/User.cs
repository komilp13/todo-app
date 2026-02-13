namespace TodoApp.Domain.Entities;

/// <summary>
/// Represents a user account in the system.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier (ULID).
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// User's email address (unique).
    /// </summary>
    public string Email { get; private set; } = null!;

    /// <summary>
    /// Hashed password using PBKDF2 or similar.
    /// </summary>
    public string PasswordHash { get; private set; } = null!;

    /// <summary>
    /// Salt used for password hashing.
    /// </summary>
    public string PasswordSalt { get; private set; } = null!;

    /// <summary>
    /// User's display name.
    /// </summary>
    public string DisplayName { get; private set; } = null!;

    /// <summary>
    /// Timestamp when user was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when user was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    /// <summary>
    /// User's tasks.
    /// </summary>
    public ICollection<TodoTask> Tasks { get; private set; } = new List<TodoTask>();

    /// <summary>
    /// User's projects.
    /// </summary>
    public ICollection<Project> Projects { get; private set; } = new List<Project>();

    /// <summary>
    /// User's labels.
    /// </summary>
    public ICollection<Label> Labels { get; private set; } = new List<Label>();

    /// <summary>
    /// Factory method to create a new user.
    /// </summary>
    public static User Create(string email, string passwordHash, string passwordSalt, string displayName)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private User() { }
}
