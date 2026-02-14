using TodoApp.Application.Features.Tasks.GetTasks;

namespace TodoApp.Application.Features.Tasks.UpdateTask;

/// <summary>
/// Response containing the updated task with full details after an update operation.
/// Reuses TaskItemDto for consistency with other task endpoints.
/// </summary>
public class UpdateTaskResponse : TaskItemDto
{
}
