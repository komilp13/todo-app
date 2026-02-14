using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TodoApp.Application.Features.Tasks.CreateTask;
using TodoApp.Domain.Interfaces;

namespace TodoApp.Api.Endpoints;

/// <summary>
/// Task management endpoints for CRUD operations on tasks.
/// </summary>
public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks")
            .WithTags("Tasks")
            .RequireAuthorization();

        group.MapPost("/", CreateTask)
            .WithName("CreateTask")
            .WithOpenApi()
            .Produces<CreateTaskResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateTask(
        CreateTaskCommand command,
        ClaimsPrincipal user,
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        // Set user ID on the command
        command.UserId = userId;

        // Validate command
        var validator = new CreateTaskCommandValidator();
        var validationResult = await validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.BadRequest(new { errors });
        }

        try
        {
            var handler = new CreateTaskHandler(taskRepository, projectRepository);
            var response = await handler.Handle(command, cancellationToken);
            return Results.Created($"/api/tasks/{response.Id}", response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
