using TodoApp.Domain.Enums;

namespace TodoApp.Application.Features.Tasks.GetTasks;

/// <summary>
/// Response containing paginated list of tasks with labels and project information.
/// </summary>
public class GetTasksResponse
{
    /// <summary>
    /// Array of tasks matching the query filters.
    /// </summary>
    public TaskItemDto[] Tasks { get; set; } = Array.Empty<TaskItemDto>();

    /// <summary>
    /// Total count of tasks matching filters.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Individual task item in the get tasks response, including associated labels and project.
/// </summary>
public class TaskItemDto
{
    /// <summary>
    /// Unique task identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Task name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Task description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Task due date.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Task priority level (P1-P4).
    /// </summary>
    public Priority Priority { get; set; }

    /// <summary>
    /// Task status (Open or Done).
    /// </summary>
    public Domain.Enums.TaskStatus Status { get; set; }

    /// <summary>
    /// GTD system list assignment.
    /// </summary>
    public SystemList SystemList { get; set; }

    /// <summary>
    /// Manual sort order within the list.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Associated project name (if task belongs to a project).
    /// </summary>
    public string? ProjectName { get; set; }

    /// <summary>
    /// Associated project ID (if task belongs to a project).
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Archive status.
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Completion timestamp (if task is completed).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Labels associated with the task.
    /// </summary>
    public LabelDto[] Labels { get; set; } = Array.Empty<LabelDto>();

    /// <summary>
    /// Timestamp when task was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when task was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Label information for task response.
/// </summary>
public class LabelDto
{
    /// <summary>
    /// Unique label identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Label name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Label color (hex format).
    /// </summary>
    public string? Color { get; set; }
}
