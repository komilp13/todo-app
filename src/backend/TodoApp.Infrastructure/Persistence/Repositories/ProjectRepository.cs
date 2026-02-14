using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Interfaces;

namespace TodoApp.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Project entity persistence.
/// </summary>
public class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProjectRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _dbContext.Projects.AddAsync(project, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Project?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Project>> GetUserProjectsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .Where(p => p.UserId == userId && p.Status == Domain.Enums.ProjectStatus.Active)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        _dbContext.Projects.Update(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await GetByIdAsync(id, Guid.Empty, cancellationToken);
        if (project != null)
        {
            _dbContext.Projects.Remove(project);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
