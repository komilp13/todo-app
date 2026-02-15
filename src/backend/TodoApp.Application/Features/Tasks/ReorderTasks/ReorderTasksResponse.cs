namespace TodoApp.Application.Features.Tasks.ReorderTasks;

/// <summary>
/// Response returned on successful task reordering, containing the updated sort orders.
/// </summary>
public class ReorderTasksResponse
{
    /// <summary>
    /// Collection of task IDs and their new sort order values.
    /// </summary>
    public ReorderedTaskDto[] ReorderedTasks { get; set; } = Array.Empty<ReorderedTaskDto>();
}

/// <summary>
/// Represents a task and its new sort order after reordering.
/// </summary>
public class ReorderedTaskDto
{
    /// <summary>
    /// Task ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// New sort order value.
    /// </summary>
    public int SortOrder { get; set; }
}
