namespace TodoApp.Domain.Entities;

/// <summary>
/// Represents a label for categorizing tasks.
/// </summary>
public class Label
{
    /// <summary>
    /// Unique identifier (ULID).
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// User who owns this label.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Label name (required, max 100 chars, unique per user).
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Optional hex color for the label (e.g., "#ff4040").
    /// </summary>
    public string? Color { get; private set; }

    /// <summary>
    /// Timestamp when label was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    /// <summary>
    /// User who owns this label.
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// Tasks with this label.
    /// </summary>
    public ICollection<TodoTask> Tasks { get; private set; } = new List<TodoTask>();

    /// <summary>
    /// Factory method to create a new label.
    /// </summary>
    public static Label Create(Guid userId, string name, string? color = null)
    {
        return new Label
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Color = color,
            CreatedAt = DateTime.UtcNow
        };
    }

    private Label() { }
}
