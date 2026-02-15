using TodoApp.Domain.Enums;

namespace TodoApp.Application.Features.Tasks.UpdateTask;

/// <summary>
/// Command to update an existing task with partial updates (only provided fields are updated).
/// </summary>
public class UpdateTaskCommand
{
    /// <summary>
    /// Task ID to update (required).
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// Updated task name (optional, max 500 characters).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Updated task description (optional, max 4000 characters).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Updated task due date (optional).
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Updated task priority level (optional, P1-P4).
    /// </summary>
    public Priority? Priority { get; set; }

    /// <summary>
    /// Updated GTD system list assignment (optional).
    /// </summary>
    public SystemList? SystemList { get; set; }

    /// <summary>
    /// Updated project ID (optional). Set to null to disassociate from project.
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// User ID of the authenticated user making the update (set by API layer).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Indicates whether ProjectId was explicitly set (to distinguish between null and not provided).
    /// </summary>
    public bool HasProjectId { get; set; }

    /// <summary>
    /// Indicates whether DueDate was explicitly set.
    /// </summary>
    public bool HasDueDate { get; set; }

    /// <summary>
    /// Indicates whether Description was explicitly set (to distinguish between null and not provided).
    /// </summary>
    public bool HasDescription { get; set; }
}
