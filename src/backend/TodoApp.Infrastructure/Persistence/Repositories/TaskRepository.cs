using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Interfaces;

namespace TodoApp.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for TodoTask entity persistence.
/// </summary>
public class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TaskRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task AddAsync(TodoTask task, CancellationToken cancellationToken = default)
    {
        await _dbContext.Tasks.AddAsync(task, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TodoTask?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TodoTask>> GetUserTasksAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tasks
            .Where(t => t.UserId == userId && !t.IsArchived && t.Status == Domain.Enums.TaskStatus.Open)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TodoTask task, CancellationToken cancellationToken = default)
    {
        _dbContext.Tasks.Update(task);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetByIdAsync(id, Guid.Empty, cancellationToken);
        if (task != null)
        {
            // Delete associated TaskLabel records first
            var taskLabels = await _dbContext.TaskLabels
                .Where(tl => tl.TaskId == id)
                .ToListAsync(cancellationToken);

            _dbContext.TaskLabels.RemoveRange(taskLabels);

            // Delete the task
            _dbContext.Tasks.Remove(task);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<int> GetMaxSortOrderAsync(Guid userId, string systemList, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<Domain.Enums.SystemList>(systemList, out var parsedList))
        {
            return 0;
        }

        var maxSortOrder = await _dbContext.Tasks
            .Where(t => t.UserId == userId && t.SystemList == parsedList && !t.IsArchived)
            .MaxAsync(t => (int?)t.SortOrder, cancellationToken);

        return maxSortOrder.HasValue ? maxSortOrder.Value + 1 : 0;
    }
}
