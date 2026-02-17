namespace TodoApp.Application.Features.Tasks.RemoveLabel;

public class RemoveLabelCommand
{
    public Guid TaskId { get; set; }
    public Guid LabelId { get; set; }
    public Guid UserId { get; set; }
}
