using TodoApp.Domain.Entities;

namespace TodoApp.Domain.Interfaces;

/// <summary>
/// Repository interface for project data access.
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// Adds a new project to the repository.
    /// </summary>
    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a project by ID, scoped to the authenticated user.
    /// </summary>
    Task<Project?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all projects for a user.
    /// </summary>
    Task<IEnumerable<Project>> GetUserProjectsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a project by ID.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
