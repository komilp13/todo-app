namespace TodoApp.Application.Features.Labels.CreateLabel;

public class CreateLabelCommand
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public Guid UserId { get; set; }
}
