using TodoApp.Domain.Enums;

namespace TodoApp.Domain.Entities;

/// <summary>
/// Represents a project (goal) that groups related tasks.
/// </summary>
public class Project
{
    /// <summary>
    /// Unique identifier (ULID).
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// User who owns this project.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Project name (required, max 100 chars).
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Project description (optional, max 4000 chars).
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Project due date (optional).
    /// </summary>
    public DateTime? DueDate { get; private set; }

    /// <summary>
    /// Project status (Active or Completed).
    /// </summary>
    public ProjectStatus Status { get; private set; }

    /// <summary>
    /// Manual sort order for display.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Timestamp when project was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Timestamp when project was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    /// <summary>
    /// User who owns this project.
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// Tasks in this project.
    /// </summary>
    public ICollection<TodoTask> Tasks { get; private set; } = new List<TodoTask>();

    /// <summary>
    /// Factory method to create a new project.
    /// </summary>
    public static Project Create(
        Guid userId,
        string name,
        string? description = null,
        DateTime? dueDate = null)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description,
            DueDate = dueDate,
            Status = ProjectStatus.Active,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private Project() { }
}
