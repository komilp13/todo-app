namespace TodoApp.Application.Features.Projects.CompleteProject;

public class CompleteProjectCommand
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
}
