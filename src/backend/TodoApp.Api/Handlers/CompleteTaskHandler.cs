using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Tasks.CompleteTask;
using TodoApp.Application.Features.Tasks.GetTasks;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

/// <summary>
/// Handler for CompleteTaskCommand that marks a task as complete with Done status and archive.
/// </summary>
public class CompleteTaskHandler
{
    private readonly ApplicationDbContext _dbContext;

    public CompleteTaskHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Marks a task as complete: sets Done status, archives it, and records completion time.
    /// Idempotent - calling multiple times is safe.
    /// Returns the updated task with full details; returns null if task not found or belongs to different user.
    /// </summary>
    public async Task<TaskItemDto?> Handle(CompleteTaskCommand command, CancellationToken cancellationToken = default)
    {
        // Fetch task with project eager-loaded, scoped to user
        var task = await _dbContext.Tasks
            .Where(t => t.Id == command.TaskId && t.UserId == command.UserId)
            .Include(t => t.Project)
            .FirstOrDefaultAsync(cancellationToken);

        if (task == null)
        {
            return null;
        }

        // Mark task as complete
        task.Complete();

        // Save changes
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Reload project in case it was updated
        await _dbContext.Entry(task).Reference(t => t.Project).LoadAsync(cancellationToken);

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
