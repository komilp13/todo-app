using TodoApp.Domain.Entities;
using TodoApp.Domain.Interfaces;

namespace TodoApp.Application.Features.Projects.CreateProject;

/// <summary>
/// Handler for CreateProjectCommand that creates a new project for the authenticated user.
/// </summary>
public class CreateProjectHandler
{
    private readonly IProjectRepository _projectRepository;

    public CreateProjectHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    /// <summary>
    /// Handles project creation by creating the project entity and saving it.
    /// New projects are placed at the top of the sort order (0).
    /// </summary>
    public async Task<CreateProjectResponse> Handle(CreateProjectCommand command, CancellationToken cancellationToken = default)
    {
        var userId = command.UserId;

        // Create new project using factory method
        var newProject = Project.Create(
            userId: userId,
            name: command.Name,
            description: command.Description,
            dueDate: command.DueDate);

        // Save project to repository
        await _projectRepository.AddAsync(newProject, cancellationToken);

        // Return response
        return new CreateProjectResponse
        {
            Id = newProject.Id,
            Name = newProject.Name,
            Description = newProject.Description,
            DueDate = newProject.DueDate,
            Status = newProject.Status,
            SortOrder = newProject.SortOrder,
            CreatedAt = newProject.CreatedAt,
            UpdatedAt = newProject.UpdatedAt
        };
    }
}
