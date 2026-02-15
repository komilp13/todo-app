using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Extensions;
using TodoApp.Api.Handlers;
using TodoApp.Application.Features.Tasks.CompleteTask;
using TodoApp.Application.Features.Tasks.CreateTask;
using TodoApp.Application.Features.Tasks.DeleteTask;
using TodoApp.Application.Features.Tasks.GetTasks;
using TodoApp.Application.Features.Tasks.ReopenTask;
using TodoApp.Application.Features.Tasks.ReorderTasks;
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

        group.MapPost("/", (HttpContext context, CreateTaskCommand command, ClaimsPrincipal user, ITaskRepository taskRepository, IProjectRepository projectRepository, CancellationToken cancellationToken) => CreateTask(context, command, user, taskRepository, projectRepository, cancellationToken))
            .WithName("CreateTask")
            .WithOpenApi()
            .Produces<CreateTaskResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/", (HttpContext context, ClaimsPrincipal user, ApplicationDbContext dbContext, [AsParameters] GetTasksQueryParams queryParams, CancellationToken cancellationToken) => GetTasks(context, user, dbContext, queryParams, cancellationToken))
            .WithName("GetTasks")
            .WithOpenApi()
            .Produces<GetTasksResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id}", (HttpContext context, string id, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => GetTaskById(context, id, user, dbContext, cancellationToken))
            .WithName("GetTaskById")
            .WithOpenApi()
            .Produces<TaskItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id}", (HttpContext context, string id, [FromBody] Dictionary<string, object?> requestBody, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => UpdateTask(context, id, requestBody, user, dbContext, cancellationToken))
            .WithName("UpdateTask")
            .WithOpenApi()
            .Produces<TaskItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{id}/complete", (HttpContext context, string id, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => CompleteTask(context, id, user, dbContext, cancellationToken))
            .WithName("CompleteTask")
            .WithOpenApi()
            .Produces<TaskItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{id}/reopen", (HttpContext context, string id, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => ReopenTask(context, id, user, dbContext, cancellationToken))
            .WithName("ReopenTask")
            .WithOpenApi()
            .Produces<TaskItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/reorder", (HttpContext context, ReorderTasksCommand command, ClaimsPrincipal user, ITaskRepository taskRepository, CancellationToken cancellationToken) => ReorderTasks(context, command, user, taskRepository, cancellationToken))
            .WithName("ReorderTasks")
            .WithOpenApi()
            .Produces<ReorderTasksResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{id}", (HttpContext context, string id, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => DeleteTask(context, id, user, dbContext, cancellationToken))
            .WithName("DeleteTask")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task CreateTask(
        HttpContext context,
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
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
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

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        try
        {
            var handler = new CreateTaskHandler(taskRepository, projectRepository);
            var response = await handler.Handle(command, cancellationToken);
            context.Response.StatusCode = StatusCodes.Status201Created;
            context.Response.Headers["Location"] = $"/api/tasks/{response.Id}";
            var json = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            var json = JsonSerializer.Serialize(new { message = ex.Message }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
        catch (InvalidOperationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { message = ex.Message }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
    }

    private static async Task GetTasks(
        HttpContext context,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        [AsParameters] GetTasksQueryParams queryParams,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Parse system list if provided
        Domain.Enums.SystemList? systemList = null;
        if (!string.IsNullOrEmpty(queryParams.SystemList))
        {
            if (!Enum.TryParse<Domain.Enums.SystemList>(queryParams.SystemList, ignoreCase: true, out var parsedSystemList))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                var json = JsonSerializer.Serialize(new { errors = new { systemList = new[] { "SystemList must be a valid value (Inbox, Next, Upcoming, or Someday)." } } }, JsonDefaults.CamelCase);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
                return;
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

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        // Execute query
        var handler = new GetTasksHandler(dbContext);
        var response = await handler.Handle(query, cancellationToken);

        var responseJson = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(responseJson);
    }

    private static async Task GetTaskById(
        HttpContext context,
        string id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Validate task ID format
        if (!Guid.TryParse(id, out var taskId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors = new { id = new[] { "Task ID must be a valid GUID." } } }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        // Execute query
        var handler = new GetTaskByIdHandler(dbContext);
        var task = await handler.Handle(taskId, userId, cancellationToken);

        if (task == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            var json = JsonSerializer.Serialize(new { message = "Task not found or does not belong to the authenticated user." }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        var responseJson = JsonSerializer.Serialize(task, JsonDefaults.CamelCase);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(responseJson);
    }

    private static async Task UpdateTask(
        HttpContext context,
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
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Validate task ID format
        if (!Guid.TryParse(id, out var taskId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors = new { id = new[] { "Task ID must be a valid GUID." } } }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
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

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        try
        {
            var handler = new UpdateTaskHandler(dbContext);
            var response = await handler.Handle(command, cancellationToken);

            if (response == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                var json = JsonSerializer.Serialize(new { message = "Task not found or does not belong to the authenticated user." }, JsonDefaults.CamelCase);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
                return;
            }

            var responseJson = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(responseJson);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { message = ex.Message }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
        catch (InvalidOperationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { message = ex.Message }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
    }

    private static async Task CompleteTask(
        HttpContext context,
        string id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Validate task ID format
        if (!Guid.TryParse(id, out var taskId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors = new { id = new[] { "Task ID must be a valid GUID." } } }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        try
        {
            var handler = new CompleteTaskHandler(dbContext);
            var response = await handler.Handle(new CompleteTaskCommand { TaskId = taskId, UserId = userId }, cancellationToken);

            if (response == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                var json = JsonSerializer.Serialize(new { message = "Task not found or does not belong to the authenticated user." }, JsonDefaults.CamelCase);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
                return;
            }

            var responseJson = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(responseJson);
        }
        catch (InvalidOperationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { message = ex.Message }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
    }

    private static async Task ReopenTask(
        HttpContext context,
        string id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Validate task ID format
        if (!Guid.TryParse(id, out var taskId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors = new { id = new[] { "Task ID must be a valid GUID." } } }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        try
        {
            var handler = new ReopenTaskHandler(dbContext);
            var response = await handler.Handle(new ReopenTaskCommand { TaskId = taskId, UserId = userId }, cancellationToken);

            if (response == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                var json = JsonSerializer.Serialize(new { message = "Task not found or does not belong to the authenticated user." }, JsonDefaults.CamelCase);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
                return;
            }

            var responseJson = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(responseJson);
        }
        catch (InvalidOperationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { message = ex.Message }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
    }

    private static async Task DeleteTask(
        HttpContext context,
        string id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Validate task ID format
        if (!Guid.TryParse(id, out var taskId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors = new { id = new[] { "Task ID must be a valid GUID." } } }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        try
        {
            var handler = new DeleteTaskHandler(dbContext);
            var deleted = await handler.Handle(new DeleteTaskCommand { TaskId = taskId, UserId = userId }, cancellationToken);

            if (!deleted)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                var json = JsonSerializer.Serialize(new { message = "Task not found or does not belong to the authenticated user." }, JsonDefaults.CamelCase);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status204NoContent;
        }
        catch (InvalidOperationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { message = ex.Message }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
    }

    private static async Task ReorderTasks(
        HttpContext context,
        ReorderTasksCommand command,
        ClaimsPrincipal user,
        ITaskRepository taskRepository,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Validate request
        var validator = new ReorderTasksCommandValidator();
        command.UserId = userId;

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            var json = JsonSerializer.Serialize(new { errors }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        try
        {
            var handler = new ReorderTasksHandler(taskRepository);
            var response = await handler.Handle(command, cancellationToken);

            context.Response.StatusCode = StatusCodes.Status200OK;
            var json = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
        catch (InvalidOperationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { message = ex.Message }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
    }
}
