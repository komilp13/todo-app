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

    /// <summary>
    /// Reorders tasks atomically by updating their sort order values.
    /// All tasks must belong to the authenticated user and the specified system list.
    /// </summary>
    /// <param name="userId">Authenticated user ID for authorization.</param>
    /// <param name="taskIds">Ordered array of task IDs. Array index becomes the sort order.</param>
    /// <param name="systemList">System list name. All tasks must belong to this list.</param>
    /// <returns>Dictionary mapping task ID to new sort order.</returns>
    /// <exception cref="InvalidOperationException">If any task is not found, doesn't belong to user, or doesn't belong to the specified system list.</exception>
    Task<Dictionary<Guid, int>> ReorderTasksAsync(Guid userId, Guid[] taskIds, string systemList, CancellationToken cancellationToken = default);
}
