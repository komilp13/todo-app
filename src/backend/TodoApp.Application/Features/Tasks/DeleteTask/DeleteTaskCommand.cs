namespace TodoApp.Application.Features.Tasks.DeleteTask;

/// <summary>
/// Command to permanently delete a task and all its associated label assignments.
/// </summary>
public class DeleteTaskCommand
{
    /// <summary>
    /// ID of the task to delete.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// ID of the authenticated user (injected from JWT claims).
    /// </summary>
    public Guid UserId { get; set; }
}
