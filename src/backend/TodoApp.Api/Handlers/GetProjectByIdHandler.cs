using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Projects.GetProject;
using TodoApp.Application.Features.Projects.GetProjects;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

public class GetProjectByIdHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetProjectByIdHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProjectItemDto?> Handle(GetProjectQuery query, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == query.ProjectId && p.UserId == query.UserId, cancellationToken);

        if (project == null)
            return null;

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
