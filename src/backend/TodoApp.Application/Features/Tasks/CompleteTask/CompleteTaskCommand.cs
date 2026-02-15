namespace TodoApp.Application.Features.Tasks.CompleteTask;

/// <summary>
/// Command to mark a task as complete (Done status, archived, with completion timestamp).
/// </summary>
public class CompleteTaskCommand
{
    /// <summary>
    /// ID of the task to complete.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// ID of the authenticated user (injected from JWT claims).
    /// </summary>
    public Guid UserId { get; set; }
}
