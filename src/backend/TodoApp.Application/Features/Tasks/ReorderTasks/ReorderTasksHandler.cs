using TodoApp.Domain.Interfaces;

namespace TodoApp.Application.Features.Tasks.ReorderTasks;

/// <summary>
/// Handler for ReorderTasksCommand that reorders tasks within a system list atomically.
/// </summary>
public class ReorderTasksHandler
{
    private readonly ITaskRepository _taskRepository;

    public ReorderTasksHandler(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    /// <summary>
    /// Handles task reordering by validating authorization and updating sort orders atomically.
    /// </summary>
    public async Task<ReorderTasksResponse> Handle(ReorderTasksCommand command, CancellationToken cancellationToken = default)
    {
        var userId = command.UserId;
        var systemList = command.SystemList.ToString();

        // Call repository to reorder tasks atomically
        // This will validate that all tasks exist, belong to the user, and belong to the system list
        var reorderedTasks = await _taskRepository.ReorderTasksAsync(userId, command.TaskIds, systemList, cancellationToken);

        // Build response
        var response = new ReorderTasksResponse
        {
            ReorderedTasks = reorderedTasks
                .Select(kvp => new ReorderedTaskDto { Id = kvp.Key, SortOrder = kvp.Value })
                .ToArray()
        };

        return response;
    }
}
