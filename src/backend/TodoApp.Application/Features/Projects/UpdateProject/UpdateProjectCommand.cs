namespace TodoApp.Application.Features.Projects.UpdateProject;

public class UpdateProjectCommand
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool HasDescription { get; set; }
    public DateTime? DueDate { get; set; }
    public bool HasDueDate { get; set; }
}
