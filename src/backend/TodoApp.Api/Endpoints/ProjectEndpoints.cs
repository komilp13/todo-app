using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Extensions;
using TodoApp.Api.Handlers;
using TodoApp.Api.Utilities;
using TodoApp.Application.Features.Projects.CompleteProject;
using TodoApp.Application.Features.Projects.CreateProject;
using TodoApp.Application.Features.Projects.DeleteProject;
using TodoApp.Application.Features.Projects.GetProject;
using TodoApp.Application.Features.Projects.GetProjects;
using TodoApp.Application.Features.Projects.ReopenProject;
using TodoApp.Application.Features.Projects.UpdateProject;
using TodoApp.Domain.Interfaces;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        group.MapPost("/", (HttpContext context, CreateProjectCommand command, ClaimsPrincipal user, IProjectRepository projectRepository, CancellationToken cancellationToken) => CreateProject(context, command, user, projectRepository, cancellationToken))
            .WithName("CreateProject")
            .WithOpenApi()
            .Produces<CreateProjectResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/", (HttpContext context, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => GetProjects(context, user, dbContext, cancellationToken))
            .WithName("GetProjects")
            .WithOpenApi()
            .Produces<GetProjectsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id}", (HttpContext context, string id, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => GetProjectById(context, id, user, dbContext, cancellationToken))
            .WithName("GetProjectById")
            .WithOpenApi()
            .Produces<ProjectItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id}", (HttpContext context, string id, [FromBody] Dictionary<string, object?> requestBody, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => UpdateProject(context, id, requestBody, user, dbContext, cancellationToken))
            .WithName("UpdateProject")
            .WithOpenApi()
            .Produces<ProjectItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{id}/complete", (HttpContext context, string id, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => CompleteProject(context, id, user, dbContext, cancellationToken))
            .WithName("CompleteProject")
            .WithOpenApi()
            .Produces<ProjectItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{id}/reopen", (HttpContext context, string id, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => ReopenProject(context, id, user, dbContext, cancellationToken))
            .WithName("ReopenProject")
            .WithOpenApi()
            .Produces<ProjectItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", (HttpContext context, string id, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => DeleteProject(context, id, user, dbContext, cancellationToken))
            .WithName("DeleteProject")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task CreateProject(
        HttpContext context,
        CreateProjectCommand command,
        ClaimsPrincipal user,
        IProjectRepository projectRepository,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        command.UserId = userId;

        var validator = new CreateProjectCommandValidator();
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
            var handler = new CreateProjectHandler(projectRepository);
            var response = await handler.Handle(command, cancellationToken);
            context.Response.StatusCode = StatusCodes.Status201Created;
            context.Response.Headers["Location"] = $"/api/projects/{response.Id}";
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

    private static async Task GetProjects(
        HttpContext context,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var query = new GetProjectsQuery { UserId = userId };

        var validator = new GetProjectsQueryValidator();
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

        var handler = new GetProjectsHandler(dbContext);
        var response = await handler.Handle(query, cancellationToken);

        var responseJson = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(responseJson);
    }

    private static async Task GetProjectById(
        HttpContext context,
        string id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!Guid.TryParse(id, out var projectId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors = new { id = new[] { "Project ID must be a valid GUID." } } }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        var handler = new GetProjectByIdHandler(dbContext);
        var project = await handler.Handle(new GetProjectQuery { ProjectId = projectId, UserId = userId }, cancellationToken);

        if (project == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            var json = JsonSerializer.Serialize(new { message = "Project not found or does not belong to the authenticated user." }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        var responseJson = JsonSerializer.Serialize(project, JsonDefaults.CamelCase);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(responseJson);
    }

    private static async Task UpdateProject(
        HttpContext context,
        string id,
        [FromBody] Dictionary<string, object?> requestBody,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!Guid.TryParse(id, out var projectId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors = new { id = new[] { "Project ID must be a valid GUID." } } }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        var command = new UpdateProjectCommand
        {
            ProjectId = projectId,
            UserId = userId,
            HasDescription = requestBody.ContainsKey("description"),
            HasDueDate = requestBody.ContainsKey("dueDate")
        };

        if (requestBody.TryGetValue("name", out var nameValue))
        {
            var nameStr = JsonValueExtractor.GetStringValue(nameValue);
            if (nameStr != null)
                command.Name = nameStr;
        }

        if (command.HasDescription && requestBody.TryGetValue("description", out var descValue))
        {
            command.Description = JsonValueExtractor.GetStringValue(descValue);
        }

        if (command.HasDueDate && requestBody.TryGetValue("dueDate", out var dueDateValue))
        {
            var dueDateStr = JsonValueExtractor.GetStringValue(dueDateValue);
            if (dueDateStr != null && DateTime.TryParse(dueDateStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dueDate))
            {
                command.DueDate = dueDate;
            }
            else if (dueDateStr == null)
            {
                command.DueDate = null;
            }
        }

        var validator = new UpdateProjectCommandValidator();
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

        var handler = new UpdateProjectHandler(dbContext);
        var response = await handler.Handle(command, cancellationToken);

        if (response == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            var json = JsonSerializer.Serialize(new { message = "Project not found or does not belong to the authenticated user." }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        var responseJson = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(responseJson);
    }

    private static async Task CompleteProject(
        HttpContext context,
        string id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!Guid.TryParse(id, out var projectId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors = new { id = new[] { "Project ID must be a valid GUID." } } }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        var handler = new CompleteProjectHandler(dbContext);
        var response = await handler.Handle(new CompleteProjectCommand { ProjectId = projectId, UserId = userId }, cancellationToken);

        if (response == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            var json = JsonSerializer.Serialize(new { message = "Project not found or does not belong to the authenticated user." }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        var responseJson = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(responseJson);
    }

    private static async Task ReopenProject(
        HttpContext context,
        string id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!Guid.TryParse(id, out var projectId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors = new { id = new[] { "Project ID must be a valid GUID." } } }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        var handler = new ReopenProjectHandler(dbContext);
        var response = await handler.Handle(new ReopenProjectCommand { ProjectId = projectId, UserId = userId }, cancellationToken);

        if (response == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            var json = JsonSerializer.Serialize(new { message = "Project not found or does not belong to the authenticated user." }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        var responseJson = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(responseJson);
    }

    private static async Task DeleteProject(
        HttpContext context,
        string id,
        ClaimsPrincipal user,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!Guid.TryParse(id, out var projectId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors = new { id = new[] { "Project ID must be a valid GUID." } } }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        var handler = new DeleteProjectHandler(dbContext);
        var deleted = await handler.Handle(new DeleteProjectCommand { ProjectId = projectId, UserId = userId }, cancellationToken);

        if (!deleted)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            var json = JsonSerializer.Serialize(new { message = "Project not found or does not belong to the authenticated user." }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }
}
