using TodoApp.Domain.Enums;

namespace TodoApp.Api.Endpoints;

/// <summary>
/// Query parameters for GetTasks endpoint.
/// Used with [AsParameters] for automatic binding from URL query string.
/// Accepts string values for enums to allow validation of invalid values.
/// </summary>
public class GetTasksQueryParams
{
    /// <summary>
    /// Filter by system list (Inbox, Next, Upcoming, Someday). Optional.
    /// Accepted as string and parsed/validated in the handler.
    /// </summary>
    public string? SystemList { get; set; }

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

    /// <summary>
    /// Converts string system list to enum, or null if invalid.
    /// </summary>
    public Domain.Enums.SystemList? GetSystemList()
    {
        if (string.IsNullOrEmpty(SystemList))
            return null;

        if (Enum.TryParse<Domain.Enums.SystemList>(SystemList, ignoreCase: true, out var result))
            return result;

        return null; // Invalid value, will be caught by validator
    }
}
