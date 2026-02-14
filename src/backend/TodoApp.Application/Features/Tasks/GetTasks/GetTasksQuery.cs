using TodoApp.Domain.Enums;

namespace TodoApp.Application.Features.Tasks.GetTasks;

/// <summary>
/// Query to retrieve tasks with optional filtering by system list, project, label, status, and archived flag.
/// </summary>
public class GetTasksQuery
{
    /// <summary>
    /// Filter by system list (Inbox, Next, Upcoming, Someday). Optional.
    /// </summary>
    public SystemList? SystemList { get; set; }

    /// <summary>
    /// Filter by project ID. Optional.
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Filter by label ID. Optional.
    /// </summary>
    public Guid? LabelId { get; set; }

    /// <summary>
    /// Filter by task status. Options: "Open", "Done", "All". Default: "Open".
    /// </summary>
    public string Status { get; set; } = "Open";

    /// <summary>
    /// Filter by archived flag. If true, returns completed tasks. Default: false (returns active tasks).
    /// </summary>
    public bool Archived { get; set; } = false;

    /// <summary>
    /// User ID of the authenticated user. Set by API layer from JWT claims.
    /// </summary>
    public Guid UserId { get; set; }
}
