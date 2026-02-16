using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Labels.DeleteLabel;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

public class DeleteLabelHandler
{
    private readonly ApplicationDbContext _dbContext;

    public DeleteLabelHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteLabelCommand command, CancellationToken cancellationToken = default)
    {
        var label = await _dbContext.Labels
            .FirstOrDefaultAsync(l => l.Id == command.LabelId && l.UserId == command.UserId, cancellationToken);

        if (label == null)
            return false;

        // Remove all TaskLabel associations (cascade delete should handle this, but be explicit)
        var taskLabels = await _dbContext.TaskLabels
            .Where(tl => tl.LabelId == command.LabelId)
            .ToListAsync(cancellationToken);

        _dbContext.TaskLabels.RemoveRange(taskLabels);
        _dbContext.Labels.Remove(label);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
