using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Tasks.GetTasks;
using TodoApp.Domain.Enums;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

/// <summary>
/// Handler for GetTasksQuery that retrieves and filters tasks for the authenticated user.
/// Executes complex query with eager loading and filtering on the database.
/// </summary>
public class GetTasksHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetTasksHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles task retrieval with optional filtering by system list, project, label, status, and archived flag.
    /// </summary>
    public async Task<GetTasksResponse> Handle(GetTasksQuery query, CancellationToken cancellationToken = default)
    {
        // Start with base query for user's tasks
        var tasksQuery = _dbContext.Tasks
            .Where(t => t.UserId == query.UserId)
            .AsQueryable();

        // Handle special "upcoming" view
        if (!string.IsNullOrEmpty(query.View) && query.View.Equals("upcoming", StringComparison.OrdinalIgnoreCase))
        {
            var now = DateTime.UtcNow;
            var upcomingThreshold = now.AddDays(14);

            // Upcoming view: only tasks with due dates within 14 days (or overdue)
            // Upcoming is a computed view — tasks are not manually moved into it
            tasksQuery = tasksQuery.Where(t =>
                !t.IsArchived &&
                t.Status == Domain.Enums.TaskStatus.Open &&
                t.DueDate.HasValue &&
                t.DueDate.Value <= upcomingThreshold
            );

            // Sort: overdue first (oldest first), then by due date ascending, then by priority
            var upcomingTasks = await tasksQuery
                .Include(t => t.Project)
                .ToListAsync(cancellationToken);

            // Get labels
            var upcomingTaskIds = upcomingTasks.Select(t => t.Id).ToList();
            var upcomingTaskLabelMappings = await _dbContext.TaskLabels
                .Where(tl => upcomingTaskIds.Contains(tl.TaskId))
                .Include(tl => tl.Label)
                .ToListAsync(cancellationToken);

            // Sort by due date ascending (overdue first), then by priority
            var sortedUpcomingTasks = upcomingTasks
                .OrderBy(t => t.DueDate!.Value)
                .ThenBy(t => t.Priority)
                .ToList();

            // Map to DTOs
            var upcomingTaskDtos = sortedUpcomingTasks.Select(task => new TaskItemDto
            {
                Id = task.Id,
                Name = task.Name,
                Description = task.Description,
                DueDate = task.DueDate,
                Priority = task.Priority,
                Status = task.Status,
                SystemList = task.SystemList,
                SortOrder = task.SortOrder,
                ProjectId = task.ProjectId,
                ProjectName = task.Project?.Name,
                IsArchived = task.IsArchived,
                CompletedAt = task.CompletedAt,
                Labels = upcomingTaskLabelMappings
                    .Where(tl => tl.TaskId == task.Id)
                    .Select(tl => new LabelDto
                    {
                        Id = tl.Label.Id,
                        Name = tl.Label.Name,
                        Color = tl.Label.Color
                    })
                    .ToArray(),
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt
            }).ToArray();

            return new GetTasksResponse
            {
                Tasks = upcomingTaskDtos,
                TotalCount = upcomingTaskDtos.Length
            };
        }

        // Apply archived filter first - it takes precedence over status filter
        if (query.Archived)
        {
            // When explicitly requesting archived, show only archived tasks
            tasksQuery = tasksQuery.Where(t => t.IsArchived);
        }
        else
        {
            // Apply status filter only if not requesting archived
            if (query.Status == "Open")
            {
                tasksQuery = tasksQuery.Where(t => t.Status == Domain.Enums.TaskStatus.Open && !t.IsArchived);
            }
            else if (query.Status == "Done")
            {
                tasksQuery = tasksQuery.Where(t => t.Status == Domain.Enums.TaskStatus.Done && t.IsArchived);
            }
            // If Status == "All", don't filter by archived status - return all tasks
        }

        // Apply system list filter
        if (query.SystemList.HasValue)
        {
            tasksQuery = tasksQuery.Where(t => t.SystemList == query.SystemList.Value);
        }

        // Apply project filter
        if (query.ProjectId.HasValue)
        {
            tasksQuery = tasksQuery.Where(t => t.ProjectId == query.ProjectId.Value);
        }

        // Apply label filter (join with TaskLabel and Label)
        if (query.LabelId.HasValue)
        {
            tasksQuery = tasksQuery
                .Where(t => _dbContext.TaskLabels
                    .Any(tl => tl.TaskId == t.Id && tl.LabelId == query.LabelId.Value));
        }

        // Get total count before sorting
        var totalCount = await tasksQuery.CountAsync(cancellationToken);

        // Sort by sort order ascending (for active tasks) or completedAt descending (for archived)
        var sortedTasks = query.Archived && query.Status == "Done"
            ? tasksQuery.OrderByDescending(t => t.CompletedAt)
            : tasksQuery.OrderBy(t => t.SortOrder);

        // Execute query with eager loading for project
        var tasks = await sortedTasks
            .Include(t => t.Project)
            .ToListAsync(cancellationToken);

        // Get labels for each task by querying TaskLabels join table
        var taskIds = tasks.Select(t => t.Id).ToList();
        var taskLabelMappings = await _dbContext.TaskLabels
            .Where(tl => taskIds.Contains(tl.TaskId))
            .Include(tl => tl.Label)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var taskDtos = tasks.Select(task => new TaskItemDto
        {
            Id = task.Id,
            Name = task.Name,
            Description = task.Description,
            DueDate = task.DueDate,
            Priority = task.Priority,
            Status = task.Status,
            SystemList = task.SystemList,
            SortOrder = task.SortOrder,
            ProjectId = task.ProjectId,
            ProjectName = task.Project?.Name,
            IsArchived = task.IsArchived,
            CompletedAt = task.CompletedAt,
            Labels = taskLabelMappings
                .Where(tl => tl.TaskId == task.Id)
                .Select(tl => new LabelDto
                {
                    Id = tl.Label.Id,
                    Name = tl.Label.Name,
                    Color = tl.Label.Color
                })
                .ToArray(),
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        }).ToArray();

        return new GetTasksResponse
        {
            Tasks = taskDtos,
            TotalCount = totalCount
        };
    }
}
