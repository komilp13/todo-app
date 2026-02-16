using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Projects.DeleteProject;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

public class DeleteProjectHandler
{
    private readonly ApplicationDbContext _dbContext;

    public DeleteProjectHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Deletes a project and orphans its tasks (sets projectId to null).
    /// Returns true if deleted; false if not found or wrong user.
    /// </summary>
    public async Task<bool> Handle(DeleteProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == command.ProjectId && p.UserId == command.UserId, cancellationToken);

        if (project == null)
            return false;

        // Orphan all tasks belonging to this project (set projectId to null)
        var tasks = await _dbContext.Tasks
            .Where(t => t.ProjectId == command.ProjectId)
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            task.ClearProject();
        }

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
