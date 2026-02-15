using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Projects.GetProjects;
using TodoApp.Domain.Enums;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

/// <summary>
/// Handler for GetProjectsQuery that retrieves all projects for the authenticated user
/// with computed task statistics (total tasks, completed tasks, completion percentage).
/// </summary>
public class GetProjectsHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetProjectsHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles project retrieval with task statistics, sorted by sort order ascending.
    /// </summary>
    public async Task<GetProjectsResponse> Handle(GetProjectsQuery query, CancellationToken cancellationToken = default)
    {
        // Get all projects for the user, sorted by sort order
        var projects = await _dbContext.Projects
            .Where(p => p.UserId == query.UserId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

        // Get task statistics for each project
        var projectIds = projects.Select(p => p.Id).ToList();
        var taskStats = await _dbContext.Tasks
            .Where(t => t.ProjectId.HasValue && projectIds.Contains(t.ProjectId.Value))
            .GroupBy(t => t.ProjectId!.Value)
            .Select(g => new
            {
                ProjectId = g.Key,
                TotalCount = g.Count(),
                CompletedCount = g.Count(t => t.IsArchived && t.Status == TodoApp.Domain.Enums.TaskStatus.Done)
            })
            .ToListAsync(cancellationToken);

        // Create a dictionary for fast lookup
        var statsDict = taskStats.ToDictionary(s => s.ProjectId);

        // Map to DTOs with statistics
        var projectDtos = projects.Select(project =>
        {
            var stats = statsDict.GetValueOrDefault(project.Id);
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
        }).ToArray();

        return new GetProjectsResponse
        {
            Projects = projectDtos,
            TotalCount = projectDtos.Length
        };
    }
}
