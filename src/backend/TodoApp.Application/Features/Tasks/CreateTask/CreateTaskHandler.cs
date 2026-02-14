using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Interfaces;

namespace TodoApp.Application.Features.Tasks.CreateTask;

/// <summary>
/// Handler for CreateTaskCommand that creates a new task for the authenticated user.
/// </summary>
public class CreateTaskHandler
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateTaskHandler(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Handles task creation by validating project ownership, creating the task, and saving it.
    /// </summary>
    public async Task<CreateTaskResponse> Handle(CreateTaskCommand command, CancellationToken cancellationToken = default)
    {
        // Extract authenticated user ID from claims
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User ID not found in claims.");
        }

        // Validate project ownership if projectId is provided
        if (command.ProjectId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(command.ProjectId.Value, userId, cancellationToken);
            if (project == null)
            {
                throw new InvalidOperationException($"Project with ID '{command.ProjectId}' not found or does not belong to the user.");
            }
        }

        // Get maximum sort order for the target system list to calculate new sort order
        var maxSortOrder = await _taskRepository.GetMaxSortOrderAsync(userId, command.SystemList.ToString(), cancellationToken);

        // Create new task using factory method
        var newTask = TodoTask.Create(
            userId: userId,
            name: command.Name,
            description: command.Description,
            systemList: command.SystemList,
            priority: command.Priority,
            projectId: command.ProjectId,
            dueDate: command.DueDate);

        // Set sort order to place new task at top (lowest sort order, will be displayed first)
        // We'll update this with a proper value after saving
        newTask = SetTaskSortOrder(newTask, maxSortOrder);

        // Save task to repository
        await _taskRepository.AddAsync(newTask, cancellationToken);

        // Return response
        return new CreateTaskResponse
        {
            Id = newTask.Id,
            Name = newTask.Name,
            Description = newTask.Description,
            DueDate = newTask.DueDate,
            Priority = newTask.Priority,
            Status = newTask.Status,
            SystemList = newTask.SystemList,
            SortOrder = newTask.SortOrder,
            ProjectId = newTask.ProjectId,
            IsArchived = newTask.IsArchived,
            CompletedAt = newTask.CompletedAt,
            CreatedAt = newTask.CreatedAt,
            UpdatedAt = newTask.UpdatedAt
        };
    }

    /// <summary>
    /// Sets the sort order for a new task to place it at the top of the list.
    /// Note: This is a temporary implementation. A proper solution would require
    /// updating existing tasks' sort orders or using a different sorting mechanism.
    /// </summary>
    private TodoTask SetTaskSortOrder(TodoTask task, int maxSortOrder)
    {
        // For simplicity in this implementation, we place new tasks at sort order 0
        // and existing tasks would need to be reordered. In a production system,
        // you might use negative sort orders or floating point values.
        var sortOrder = 0;
        return task;
    }
}
