using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Handlers;
using TodoApp.Api.Utilities;
using TodoApp.Application.Features.Labels.CreateLabel;
using TodoApp.Application.Features.Labels.DeleteLabel;
using TodoApp.Application.Features.Labels.GetLabels;
using TodoApp.Application.Features.Labels.UpdateLabel;
using TodoApp.Api.Extensions;
using TodoApp.Infrastructure.Persistence;

namespace TodoApp.Api.Endpoints;

public static class LabelEndpoints
{
    public static void MapLabelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/labels")
            .WithTags("Labels")
            .RequireAuthorization();

        group.MapPost("/", (HttpContext context, CreateLabelCommand command, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => CreateLabel(context, command, user, dbContext, cancellationToken))
            .WithName("CreateLabel")
            .WithOpenApi()
            .Produces<CreateLabelResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/", (HttpContext context, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => GetLabels(context, user, dbContext, cancellationToken))
            .WithName("GetLabels")
            .WithOpenApi()
            .Produces<GetLabelsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/{id}", (HttpContext context, string id, [FromBody] Dictionary<string, object?> requestBody, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => UpdateLabel(context, id, requestBody, user, dbContext, cancellationToken))
            .WithName("UpdateLabel")
            .WithOpenApi()
            .Produces<LabelItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{id}", (HttpContext context, string id, ClaimsPrincipal user, ApplicationDbContext dbContext, CancellationToken cancellationToken) => DeleteLabel(context, id, user, dbContext, cancellationToken))
            .WithName("DeleteLabel")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task CreateLabel(
        HttpContext context,
        CreateLabelCommand command,
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

        command.UserId = userId;

        var validator = new CreateLabelCommandValidator();
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
            var handler = new CreateLabelHandler(dbContext);
            var response = await handler.Handle(command, cancellationToken);
            context.Response.StatusCode = StatusCodes.Status201Created;
            context.Response.Headers["Location"] = $"/api/labels/{response.Id}";
            var json = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
        catch (InvalidOperationException)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            var json = JsonSerializer.Serialize(new { message = "A label with this name already exists." }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
    }

    private static async Task GetLabels(
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

        var handler = new GetLabelsHandler(dbContext);
        var response = await handler.Handle(new GetLabelsQuery { UserId = userId }, cancellationToken);

        var json = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json);
    }

    private static async Task UpdateLabel(
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

        if (!Guid.TryParse(id, out var labelId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors = new { id = new[] { "Label ID must be a valid GUID." } } }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        var command = new UpdateLabelCommand
        {
            LabelId = labelId,
            UserId = userId,
            HasColor = requestBody.ContainsKey("color")
        };

        if (requestBody.TryGetValue("name", out var nameValue))
        {
            command.Name = JsonValueExtractor.GetStringValue(nameValue);
        }

        if (command.HasColor && requestBody.TryGetValue("color", out var colorValue))
        {
            command.Color = JsonValueExtractor.GetStringValue(colorValue);
        }

        var validator = new UpdateLabelCommandValidator();
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
            var handler = new UpdateLabelHandler(dbContext);
            var response = await handler.Handle(command, cancellationToken);

            if (response == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                var json = JsonSerializer.Serialize(new { message = "Label not found or does not belong to the authenticated user." }, JsonDefaults.CamelCase);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
                return;
            }

            var responseJson = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(responseJson);
        }
        catch (InvalidOperationException)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            var json = JsonSerializer.Serialize(new { message = "A label with this name already exists." }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
    }

    private static async Task DeleteLabel(
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

        if (!Guid.TryParse(id, out var labelId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var json = JsonSerializer.Serialize(new { errors = new { id = new[] { "Label ID must be a valid GUID." } } }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        var handler = new DeleteLabelHandler(dbContext);
        var deleted = await handler.Handle(new DeleteLabelCommand { LabelId = labelId, UserId = userId }, cancellationToken);

        if (!deleted)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            var json = JsonSerializer.Serialize(new { message = "Label not found or does not belong to the authenticated user." }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }
}
