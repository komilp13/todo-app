namespace TodoApp.Application.Features.Tasks.ReopenTask;

/// <summary>
/// Command to reopen a completed task (Open status, unarchived, clears completion timestamp).
/// </summary>
public class ReopenTaskCommand
{
    /// <summary>
    /// ID of the task to reopen.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// ID of the authenticated user (injected from JWT claims).
    /// </summary>
    public Guid UserId { get; set; }
}
