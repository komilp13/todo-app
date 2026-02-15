namespace TodoApp.Application.Features.Projects.GetProjects;

/// <summary>
/// Query to retrieve all projects for the authenticated user with task statistics.
/// </summary>
public class GetProjectsQuery
{
    /// <summary>
    /// User ID of the authenticated user. Set by API layer from JWT claims.
    /// </summary>
    public Guid UserId { get; set; }
}
