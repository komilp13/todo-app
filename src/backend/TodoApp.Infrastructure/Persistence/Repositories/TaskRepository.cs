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

    /// <inheritdoc />
    public async Task<Dictionary<Guid, int>> ReorderTasksAsync(Guid userId, Guid[] taskIds, string systemList, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<Domain.Enums.SystemList>(systemList, out var parsedList))
        {
            throw new InvalidOperationException($"Invalid system list: {systemList}");
        }

        // Fetch all tasks by IDs
        var tasks = await _dbContext.Tasks
            .Where(t => taskIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        // Validate: all task IDs were found
        if (tasks.Count != taskIds.Length)
        {
            var foundIds = tasks.Select(t => t.Id).ToHashSet();
            var missingIds = taskIds.Where(id => !foundIds.Contains(id)).ToList();
            throw new InvalidOperationException($"Tasks not found: {string.Join(", ", missingIds)}");
        }

        // Validate: all tasks belong to the user
        if (tasks.Any(t => t.UserId != userId))
        {
            throw new InvalidOperationException("One or more tasks do not belong to the authenticated user.");
        }

        // Validate: all tasks belong to the specified system list
        if (tasks.Any(t => t.SystemList != parsedList))
        {
            throw new InvalidOperationException($"One or more tasks do not belong to the {systemList} system list.");
        }

        // Update sort orders atomically
        var result = new Dictionary<Guid, int>();
        for (int i = 0; i < taskIds.Length; i++)
        {
            var task = tasks.First(t => t.Id == taskIds[i]);
            task.UpdateSortOrder(i);
            result[task.Id] = task.SortOrder;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return result;
    }
}
