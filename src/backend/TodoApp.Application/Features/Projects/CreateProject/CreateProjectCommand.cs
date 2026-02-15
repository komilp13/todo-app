namespace TodoApp.Application.Features.Projects.CreateProject;

/// <summary>
/// Command to create a new project for the authenticated user.
/// </summary>
public class CreateProjectCommand
{
    /// <summary>
    /// Project name (required, max 200 characters).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project description (optional, max 4000 characters).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Project due date (optional).
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// User ID of the authenticated user creating the project.
    /// This is set by the API layer when extracting from JWT claims.
    /// </summary>
    public Guid UserId { get; set; }
}
