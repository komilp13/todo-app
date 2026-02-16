using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Labels.GetLabels;
using TodoApp.Application.Features.Labels.UpdateLabel;
using TaskStatus = TodoApp.Domain.Enums.TaskStatus;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

public class UpdateLabelHandler
{
    private readonly ApplicationDbContext _dbContext;

    public UpdateLabelHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LabelItemDto?> Handle(UpdateLabelCommand command, CancellationToken cancellationToken = default)
    {
        var label = await _dbContext.Labels
            .FirstOrDefaultAsync(l => l.Id == command.LabelId && l.UserId == command.UserId, cancellationToken);

        if (label == null)
            return null;

        if (command.Name != null)
        {
            // Check for duplicate name (case-insensitive), excluding self
            var duplicate = await _dbContext.Labels
                .AnyAsync(l => l.UserId == command.UserId
                    && l.Id != command.LabelId
                    && l.Name.ToLower() == command.Name.ToLower(), cancellationToken);

            if (duplicate)
            {
                throw new InvalidOperationException("A label with this name already exists.");
            }

            label.UpdateName(command.Name);
        }

        if (command.HasColor)
        {
            label.UpdateColor(command.Color);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var taskCount = await _dbContext.TaskLabels
            .CountAsync(tl => tl.LabelId == label.Id
                && _dbContext.Tasks.Any(t => t.Id == tl.TaskId && !t.IsArchived && t.Status == TaskStatus.Open), cancellationToken);

        return new LabelItemDto
        {
            Id = label.Id,
            Name = label.Name,
            Color = label.Color,
            TaskCount = taskCount,
            CreatedAt = label.CreatedAt
        };
    }
}
