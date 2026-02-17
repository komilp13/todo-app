namespace TodoApp.Application.Features.Tasks.AssignLabel;

public class AssignLabelCommand
{
    public Guid TaskId { get; set; }
    public Guid LabelId { get; set; }
    public Guid UserId { get; set; }
}
