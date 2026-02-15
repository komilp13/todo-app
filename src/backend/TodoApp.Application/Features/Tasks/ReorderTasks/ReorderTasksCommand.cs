using TodoApp.Domain.Enums;

namespace TodoApp.Application.Features.Tasks.ReorderTasks;

/// <summary>
/// Command to reorder tasks within a specific system list.
/// Accepts an ordered array of task IDs and updates their sort order values atomically.
/// </summary>
public class ReorderTasksCommand
{
    /// <summary>
    /// Array of task IDs in the desired order. Index 0 will have sortOrder 0, etc.
    /// </summary>
    public Guid[] TaskIds { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// The system list to which all tasks must belong.
    /// </summary>
    public SystemList SystemList { get; set; }

    /// <summary>
    /// User ID of the authenticated user performing the reorder.
    /// This is set by the API layer when extracting from JWT claims.
    /// </summary>
    public Guid UserId { get; set; }
}
