namespace TodoApp.Application.Features.Projects.GetProject;

public class GetProjectQuery
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
}
