using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Extensions;
using TodoApp.Api.Handlers;
using TodoApp.Application.Features.Projects.CreateProject;
using TodoApp.Application.Features.Projects.GetProjects;
using TodoApp.Domain.Interfaces;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Endpoints;

/// <summary>
/// Project management endpoints for CRUD operations on projects.
/// </summary>
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
    }

    private static async Task CreateProject(
        HttpContext context,
        CreateProjectCommand command,
        ClaimsPrincipal user,
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
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Create query
        var query = new GetProjectsQuery
        {
            UserId = userId
        };

        // Validate query
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

        // Execute query
        var handler = new GetProjectsHandler(dbContext);
        var response = await handler.Handle(query, cancellationToken);

        var responseJson = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(responseJson);
    }
}
