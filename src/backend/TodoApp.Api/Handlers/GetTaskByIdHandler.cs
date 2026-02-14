using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Tasks.GetTasks;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

/// <summary>
/// Handler for retrieving a single task by ID with full details including labels and project info.
/// Ensures the task belongs to the authenticated user.
/// </summary>
public class GetTaskByIdHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetTaskByIdHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retrieves a single task by ID with associated labels and project information.
    /// Returns null if task is not found or belongs to a different user.
    /// </summary>
    public async Task<TaskItemDto?> Handle(Guid taskId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Fetch task with project eager-loaded, scoped to user
        var task = await _dbContext.Tasks
            .Where(t => t.Id == taskId && t.UserId == userId)
            .Include(t => t.Project)
            .FirstOrDefaultAsync(cancellationToken);

        if (task == null)
        {
            return null;
        }

        // Get labels for this task
        var labels = await _dbContext.TaskLabels
            .Where(tl => tl.TaskId == task.Id)
            .Include(tl => tl.Label)
            .Select(tl => new LabelDto
            {
                Id = tl.Label.Id,
                Name = tl.Label.Name,
                Color = tl.Label.Color
            })
            .ToArrayAsync(cancellationToken);

        // Map to DTO
        return new TaskItemDto
        {
            Id = task.Id,
            Name = task.Name,
            Description = task.Description,
            DueDate = task.DueDate,
            Priority = task.Priority,
            Status = task.Status,
            SystemList = task.SystemList,
            SortOrder = task.SortOrder,
            ProjectId = task.ProjectId,
            ProjectName = task.Project?.Name,
            IsArchived = task.IsArchived,
            CompletedAt = task.CompletedAt,
            Labels = labels,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
