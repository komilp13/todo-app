using TodoApp.Domain.Enums;

namespace TodoApp.Application.Features.Tasks.CreateTask;

/// <summary>
/// Response returned on successful task creation, containing full task details.
/// </summary>
public class CreateTaskResponse
{
    /// <summary>
    /// Unique identifier of the created task.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Task name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Task description (if provided).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Task due date (if provided).
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Task priority level.
    /// </summary>
    public Priority Priority { get; set; }

    /// <summary>
    /// Task status (always Open for newly created tasks).
    /// </summary>
    public TaskStatus Status { get; set; }

    /// <summary>
    /// GTD system list assignment.
    /// </summary>
    public SystemList SystemList { get; set; }

    /// <summary>
    /// Manual sort order within the list.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Associated project ID (if assigned).
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Archive status (always false for newly created tasks).
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Completion timestamp (null for newly created tasks).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Timestamp when task was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when task was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
