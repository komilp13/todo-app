namespace TodoApp.Application.Features.Labels.DeleteLabel;

public class DeleteLabelCommand
{
    public Guid LabelId { get; set; }
    public Guid UserId { get; set; }
}
