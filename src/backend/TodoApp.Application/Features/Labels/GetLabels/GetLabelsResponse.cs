namespace TodoApp.Application.Features.Labels.GetLabels;

public class GetLabelsResponse
{
    public List<LabelItemDto> Labels { get; set; } = new();
    public int TotalCount { get; set; }
}

public class LabelItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public int TaskCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
