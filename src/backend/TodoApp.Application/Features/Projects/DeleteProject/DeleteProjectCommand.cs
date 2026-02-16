namespace TodoApp.Application.Features.Projects.DeleteProject;

public class DeleteProjectCommand
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
}
