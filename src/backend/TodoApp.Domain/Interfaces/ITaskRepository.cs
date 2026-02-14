using TodoApp.Domain.Entities;

namespace TodoApp.Domain.Interfaces;

/// <summary>
/// Repository interface for task data access.
/// </summary>
public interface ITaskRepository
{
    /// <summary>
    /// Adds a new task to the repository.
    /// </summary>
    Task AddAsync(TodoTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a task by ID, scoped to the authenticated user.
    /// </summary>
    Task<TodoTask?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tasks for a user with optional filtering.
    /// </summary>
    Task<IEnumerable<TodoTask>> GetUserTasksAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    Task UpdateAsync(TodoTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a task by ID.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the maximum sort order for tasks in a system list.
    /// </summary>
    Task<int> GetMaxSortOrderAsync(Guid userId, string systemList, CancellationToken cancellationToken = default);
}
