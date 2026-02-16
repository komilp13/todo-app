using TodoApp.Domain.Enums;

namespace TodoApp.Application.Features.Tasks.CreateTask;

/// <summary>
/// Command to create a new task for the authenticated user.
/// </summary>
public class CreateTaskCommand
{
    /// <summary>
    /// Task name (required, max 500 characters).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Task description (optional, max 4000 characters).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Task due date (optional).
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Task priority level (P1-P4). Optional, defaults to no priority.
    /// </summary>
    public Priority? Priority { get; set; }

    /// <summary>
    /// GTD system list assignment. Defaults to Inbox.
    /// </summary>
    public SystemList SystemList { get; set; } = SystemList.Inbox;

    /// <summary>
    /// Optional project ID to associate task with a project.
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// User ID of the authenticated user creating the task.
    /// This is set by the API layer when extracting from JWT claims.
    /// </summary>
    public Guid UserId { get; set; }
}
