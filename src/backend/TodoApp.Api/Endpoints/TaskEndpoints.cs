using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Handlers;
using TodoApp.Application.Features.Tasks.CompleteTask;
using TodoApp.Application.Features.Tasks.CreateTask;
using TodoApp.Application.Features.Tasks.DeleteTask;
using TodoApp.Application.Features.Tasks.GetTasks;
using TodoApp.Application.Features.Tasks.ReopenTask;
using TodoApp.Application.Features.Tasks.UpdateTask;
using TodoApp.Domain.Enums;
using TodoApp.Domain.Interfaces;
using TodoApp.Infrastructure.Persistence;

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

        group.MapGet("/", GetTasks)
            .WithName("GetTasks")
            .WithOpenApi()
            .Produces<GetTasksResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id}", GetTaskById)
            .WithName("GetTaskById")
            .WithOpenApi()
            .Produces<TaskItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id}", UpdateTask)
            .WithName("UpdateTask")
            .WithOpenApi()
            .Produces<TaskItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{id}/complete", CompleteTask)
            .WithName("CompleteTask")
            .WithOpenApi()
            .Produces<TaskItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{id}/reopen", ReopenTask)
            .WithName("ReopenTask")
            .WithOpenApi()
            .Produces<TaskItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", DeleteTask)
            .WithName("DeleteTask")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
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

    private static async Task<IResult> GetTasks(
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        [AsParameters] GetTasksQueryParams queryParams,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        // Parse system list if provided
        Domain.Enums.SystemList? systemList = null;
        if (!string.IsNullOrEmpty(queryParams.SystemList))
        {
            if (!Enum.TryParse<Domain.Enums.SystemList>(queryParams.SystemList, ignoreCase: true, out var parsedSystemList))
            {
                return Results.BadRequest(new { errors = new { systemList = new[] { "SystemList must be a valid value (Inbox, Next, Upcoming, or Someday)." } } });
            }
            systemList = parsedSystemList;
        }

        // Create query from parameters
        var query = new GetTasksQuery
        {
            SystemList = systemList,
            ProjectId = queryParams.ProjectId,
            LabelId = queryParams.LabelId,
            Status = queryParams.Status ?? "Open",
            Archived = queryParams.Archived ?? false,
            UserId = userId
        };

        // Validate query
        var validator = new GetTasksQueryValidator();
        var validationResult = await validator.ValidateAsync(query, cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.BadRequest(new { errors });
        }

        // Execute query
        var handler = new GetTasksHandler(dbContext);
        var response = await handler.Handle(query, cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetTaskById(
        string id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        // Validate task ID format
        if (!Guid.TryParse(id, out var taskId))
        {
            return Results.BadRequest(new { errors = new { id = new[] { "Task ID must be a valid GUID." } } });
        }

        // Execute query
        var handler = new GetTaskByIdHandler(dbContext);
        var task = await handler.Handle(taskId, userId, cancellationToken);

        if (task == null)
        {
            return Results.NotFound(new { message = "Task not found or does not belong to the authenticated user." });
        }

        return Results.Ok(task);
    }

    private static async Task<IResult> UpdateTask(
        string id,
        [FromBody] Dictionary<string, object?> requestBody,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        // Validate task ID format
        if (!Guid.TryParse(id, out var taskId))
        {
            return Results.BadRequest(new { errors = new { id = new[] { "Task ID must be a valid GUID." } } });
        }

        // Build command from request body (only include provided fields)
        var command = new UpdateTaskCommand
        {
            TaskId = taskId,
            UserId = userId,
            HasProjectId = requestBody.ContainsKey("projectId"),
            HasDueDate = requestBody.ContainsKey("dueDate")
        };

        // Parse optional fields if provided
        if (requestBody.TryGetValue("name", out var nameValue) && nameValue is string name)
        {
            command.Name = name;
        }

        if (requestBody.TryGetValue("description", out var descValue))
        {
            command.Description = descValue as string;
        }

        if (requestBody.TryGetValue("dueDate", out var dueDateValue) && dueDateValue is string dueDateStr)
        {
            if (DateTime.TryParse(dueDateStr, out var dueDate))
            {
                command.DueDate = dueDate;
            }
        }

        if (requestBody.TryGetValue("priority", out var priorityValue) && priorityValue is string priorityStr)
        {
            if (Enum.TryParse<Priority>(priorityStr, ignoreCase: true, out var priority))
            {
                command.Priority = priority;
            }
        }

        if (requestBody.TryGetValue("systemList", out var systemListValue) && systemListValue is string systemListStr)
        {
            if (Enum.TryParse<Domain.Enums.SystemList>(systemListStr, ignoreCase: true, out var systemList))
            {
                command.SystemList = systemList;
            }
        }

        if (requestBody.TryGetValue("projectId", out var projectIdValue))
        {
            if (projectIdValue is string projectIdStr && !string.IsNullOrEmpty(projectIdStr))
            {
                if (Guid.TryParse(projectIdStr, out var projectId))
                {
                    command.ProjectId = projectId;
                }
            }
            else if (projectIdValue == null)
            {
                command.ProjectId = null; // Explicitly clear project
            }
        }

        // Validate command
        var validator = new UpdateTaskCommandValidator();
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
            var handler = new UpdateTaskHandler(dbContext);
            var response = await handler.Handle(command, cancellationToken);

            if (response == null)
            {
                return Results.NotFound(new { message = "Task not found or does not belong to the authenticated user." });
            }

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Results.BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> CompleteTask(
        string id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        // Validate task ID format
        if (!Guid.TryParse(id, out var taskId))
        {
            return Results.BadRequest(new { errors = new { id = new[] { "Task ID must be a valid GUID." } } });
        }

        try
        {
            var handler = new CompleteTaskHandler(dbContext);
            var response = await handler.Handle(new CompleteTaskCommand { TaskId = taskId, UserId = userId }, cancellationToken);

            if (response == null)
            {
                return Results.NotFound(new { message = "Task not found or does not belong to the authenticated user." });
            }

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> ReopenTask(
        string id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        // Validate task ID format
        if (!Guid.TryParse(id, out var taskId))
        {
            return Results.BadRequest(new { errors = new { id = new[] { "Task ID must be a valid GUID." } } });
        }

        try
        {
            var handler = new ReopenTaskHandler(dbContext);
            var response = await handler.Handle(new ReopenTaskCommand { TaskId = taskId, UserId = userId }, cancellationToken);

            if (response == null)
            {
                return Results.NotFound(new { message = "Task not found or does not belong to the authenticated user." });
            }

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteTask(
        string id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        // Validate task ID format
        if (!Guid.TryParse(id, out var taskId))
        {
            return Results.BadRequest(new { errors = new { id = new[] { "Task ID must be a valid GUID." } } });
        }

        try
        {
            var handler = new DeleteTaskHandler(dbContext);
            var deleted = await handler.Handle(new DeleteTaskCommand { TaskId = taskId, UserId = userId }, cancellationToken);

            if (!deleted)
            {
                return Results.NotFound(new { message = "Task not found or does not belong to the authenticated user." });
            }

            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
