using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Tasks.DeleteTask;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

/// <summary>
/// Handler for DeleteTaskCommand that permanently deletes a task and its label associations.
/// </summary>
public class DeleteTaskHandler
{
    private readonly ApplicationDbContext _dbContext;

    public DeleteTaskHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Permanently deletes a task and all its associated TaskLabel records.
    /// TaskLabel cascade delete is handled by EF Core based on configuration.
    /// Returns true if task was found and deleted; false if task not found or belongs to different user.
    /// </summary>
    public async Task<bool> Handle(DeleteTaskCommand command, CancellationToken cancellationToken = default)
    {
        // Fetch task scoped to user
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == command.TaskId && t.UserId == command.UserId, cancellationToken);

        if (task == null)
        {
            return false;
        }

        // Remove the task (TaskLabel records will be cascade deleted by EF Core)
        _dbContext.Tasks.Remove(task);

        // Save changes
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
