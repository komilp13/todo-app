using TodoApp.Domain.Enums;

namespace TodoApp.Api.Endpoints;

/// <summary>
/// Query parameters for GetTasks endpoint.
/// Used with [AsParameters] for automatic binding from URL query string.
/// </summary>
public class GetTasksQueryParams
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
    public string? Status { get; set; }

    /// <summary>
    /// Filter by archived flag. If true, returns completed tasks. Default: false.
    /// </summary>
    public bool? Archived { get; set; }
}
