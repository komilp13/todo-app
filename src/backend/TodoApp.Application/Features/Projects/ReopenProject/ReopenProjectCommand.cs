namespace TodoApp.Application.Features.Projects.ReopenProject;

public class ReopenProjectCommand
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
}
