namespace TodoApp.Application.Features.Labels.CreateLabel;

public class CreateLabelResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; }
}
