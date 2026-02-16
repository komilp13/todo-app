using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Labels.GetLabels;
using TaskStatus = TodoApp.Domain.Enums.TaskStatus;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

public class GetLabelsHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetLabelsHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetLabelsResponse> Handle(GetLabelsQuery query, CancellationToken cancellationToken = default)
    {
        var labels = await _dbContext.Labels
            .Where(l => l.UserId == query.UserId)
            .OrderBy(l => l.Name)
            .Select(l => new LabelItemDto
            {
                Id = l.Id,
                Name = l.Name,
                Color = l.Color,
                CreatedAt = l.CreatedAt,
                TaskCount = _dbContext.TaskLabels
                    .Count(tl => tl.LabelId == l.Id
                        && _dbContext.Tasks.Any(t => t.Id == tl.TaskId && !t.IsArchived && t.Status == TaskStatus.Open))
            })
            .ToListAsync(cancellationToken);

        return new GetLabelsResponse
        {
            Labels = labels,
            TotalCount = labels.Count
        };
    }
}
