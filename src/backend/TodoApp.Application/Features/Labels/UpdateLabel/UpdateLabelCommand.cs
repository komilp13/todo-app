namespace TodoApp.Application.Features.Labels.UpdateLabel;

public class UpdateLabelCommand
{
    public Guid LabelId { get; set; }
    public Guid UserId { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public bool HasColor { get; set; }
}
