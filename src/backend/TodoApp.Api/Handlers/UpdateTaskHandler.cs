using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Tasks.GetTasks;
using TodoApp.Application.Features.Tasks.UpdateTask;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

/// <summary>
/// Handler for UpdateTaskCommand that updates a task with partial updates.
/// Only updates fields that are explicitly provided in the command.
/// </summary>
public class UpdateTaskHandler
{
    private readonly ApplicationDbContext _dbContext;

    public UpdateTaskHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Updates a task with only the provided fields, leaving others unchanged.
    /// Refreshes the UpdatedAt timestamp on every update.
    /// Validates that ProjectId (if provided) references a user-owned project.
    /// Returns null if task is not found or belongs to a different user.
    /// </summary>
    public async Task<TaskItemDto?> Handle(UpdateTaskCommand command, CancellationToken cancellationToken = default)
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

        // If ProjectId is being set, validate it belongs to the user and exists
        if (command.HasProjectId && command.ProjectId.HasValue)
        {
            var projectExists = await _dbContext.Projects
                .AnyAsync(p => p.Id == command.ProjectId.Value && p.UserId == command.UserId, cancellationToken);

            if (!projectExists)
            {
                throw new InvalidOperationException($"Project with ID {command.ProjectId} not found or does not belong to the user.");
            }
        }

        // Update only provided fields using entity methods
        if (!string.IsNullOrEmpty(command.Name))
        {
            task.UpdateName(command.Name);
        }

        if (command.HasDescription)
        {
            task.UpdateDescription(command.Description);
        }

        if (command.Priority.HasValue)
        {
            task.UpdatePriority(command.Priority.Value);
        }

        if (command.SystemList.HasValue)
        {
            task.UpdateSystemList(command.SystemList.Value);
        }

        if (command.HasDueDate)
        {
            task.UpdateDueDate(command.DueDate);
        }

        if (command.HasProjectId)
        {
            task.UpdateProjectId(command.ProjectId);
        }

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
