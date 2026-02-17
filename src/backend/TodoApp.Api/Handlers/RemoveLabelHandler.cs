using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Tasks.GetTasks;
using TodoApp.Application.Features.Tasks.RemoveLabel;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

public class RemoveLabelHandler
{
    private readonly ApplicationDbContext _dbContext;

    public RemoveLabelHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TaskItemDto?> Handle(RemoveLabelCommand command, CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.Tasks
            .Where(t => t.Id == command.TaskId && t.UserId == command.UserId)
            .Include(t => t.Project)
            .FirstOrDefaultAsync(cancellationToken);

        if (task == null)
            return null;

        var labelExists = await _dbContext.Labels
            .AnyAsync(l => l.Id == command.LabelId && l.UserId == command.UserId, cancellationToken);

        if (!labelExists)
            return null;

        var taskLabel = await _dbContext.TaskLabels
            .FirstOrDefaultAsync(tl => tl.TaskId == command.TaskId && tl.LabelId == command.LabelId, cancellationToken);

        if (taskLabel != null)
        {
            _dbContext.TaskLabels.Remove(taskLabel);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

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
