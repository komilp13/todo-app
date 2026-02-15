using TodoApp.Domain.Enums;

namespace TodoApp.Application.Features.Projects.CreateProject;

/// <summary>
/// Response DTO returned after successfully creating a project.
/// </summary>
public class CreateProjectResponse
{
    /// <summary>
    /// Unique identifier for the created project.
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
    /// Timestamp when project was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when project was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
