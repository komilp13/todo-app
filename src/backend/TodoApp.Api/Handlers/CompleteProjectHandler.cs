using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Projects.CompleteProject;
using TodoApp.Application.Features.Projects.GetProjects;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

public class CompleteProjectHandler
{
    private readonly ApplicationDbContext _dbContext;

    public CompleteProjectHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProjectItemDto?> Handle(CompleteProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == command.ProjectId && p.UserId == command.UserId, cancellationToken);

        if (project == null)
            return null;

        project.Complete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get task statistics
        var stats = await _dbContext.Tasks
            .Where(t => t.ProjectId == project.Id)
            .GroupBy(t => 1)
            .Select(g => new
            {
                TotalCount = g.Count(),
                CompletedCount = g.Count(t => t.IsArchived && t.Status == TodoApp.Domain.Enums.TaskStatus.Done)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totalCount = stats?.TotalCount ?? 0;
        var completedCount = stats?.CompletedCount ?? 0;
        var completionPercentage = totalCount > 0
            ? (int)Math.Round((double)completedCount / totalCount * 100)
            : 0;

        return new ProjectItemDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            DueDate = project.DueDate,
            Status = project.Status,
            SortOrder = project.SortOrder,
            TotalTaskCount = totalCount,
            CompletedTaskCount = completedCount,
            CompletionPercentage = completionPercentage,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }
}
