using TodoApp.Domain.Enums;

namespace TodoApp.Application.Features.Projects.GetProjects;

/// <summary>
/// Response DTO containing all projects for the authenticated user with task statistics.
/// </summary>
public class GetProjectsResponse
{
    /// <summary>
    /// Array of projects with statistics.
    /// </summary>
    public ProjectItemDto[] Projects { get; set; } = Array.Empty<ProjectItemDto>();

    /// <summary>
    /// Total number of projects returned.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// DTO representing a single project with task statistics.
/// </summary>
public class ProjectItemDto
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Project name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project description (optional).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Project due date (optional).
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Project status (Active or Completed).
    /// </summary>
    public ProjectStatus Status { get; set; }

    /// <summary>
    /// Manual sort order.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Total number of tasks in this project.
    /// </summary>
    public int TotalTaskCount { get; set; }

    /// <summary>
    /// Number of completed (archived) tasks in this project.
    /// </summary>
    public int CompletedTaskCount { get; set; }

    /// <summary>
    /// Completion percentage (0-100).
    /// </summary>
    public int CompletionPercentage { get; set; }

    /// <summary>
    /// Timestamp when project was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when project was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
