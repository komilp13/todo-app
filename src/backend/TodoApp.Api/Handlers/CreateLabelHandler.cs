using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Features.Labels.CreateLabel;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Handlers;

public class CreateLabelHandler
{
    private readonly ApplicationDbContext _dbContext;

    public CreateLabelHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateLabelResponse> Handle(CreateLabelCommand command, CancellationToken cancellationToken = default)
    {
        // Check for duplicate name (case-insensitive)
        var duplicate = await _dbContext.Labels
            .AnyAsync(l => l.UserId == command.UserId && l.Name.ToLower() == command.Name.ToLower(), cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("A label with this name already exists.");
        }

        var label = Label.Create(command.UserId, command.Name, command.Color);
        _dbContext.Labels.Add(label);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateLabelResponse
        {
            Id = label.Id,
            Name = label.Name,
            Color = label.Color,
            CreatedAt = label.CreatedAt
        };
    }
}
