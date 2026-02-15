using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using TodoApp.Api.Extensions;
using TodoApp.Application.Features.Auth.CurrentUser;
using TodoApp.Application.Features.Auth.Login;
using TodoApp.Application.Features.Auth.Register;
using TodoApp.Domain.Interfaces;

namespace TodoApp.Api.Endpoints;

/// <summary>
/// Authentication endpoints for user registration and login.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", (HttpContext context, RegisterCommand command, IUserRepository userRepository, IPasswordHashingService passwordHashingService, IJwtTokenService jwtTokenService, CancellationToken cancellationToken) => Register(context, command, userRepository, passwordHashingService, jwtTokenService, cancellationToken))
            .WithName("Register")
            .WithOpenApi()
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/login", (HttpContext context, LoginCommand command, IUserRepository userRepository, IPasswordHashingService passwordHashingService, IJwtTokenService jwtTokenService, CancellationToken cancellationToken) => Login(context, command, userRepository, passwordHashingService, jwtTokenService, cancellationToken))
            .WithName("Login")
            .WithOpenApi()
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", (HttpContext context, ClaimsPrincipal user, IUserRepository userRepository, CancellationToken cancellationToken) => GetCurrentUser(context, user, userRepository, cancellationToken))
            .WithName("GetCurrentUser")
            .WithOpenApi()
            .RequireAuthorization()
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task Register(
        HttpContext context,
        RegisterCommand command,
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        IJwtTokenService jwtTokenService,
        CancellationToken cancellationToken)
    {
        // Validate command
        var validator = new RegisterCommandValidator();
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
            var handler = new RegisterHandler(userRepository, passwordHashingService, jwtTokenService);
            var response = await handler.Handle(command, cancellationToken);
            context.Response.StatusCode = StatusCodes.Status201Created;
            context.Response.Headers["Location"] = $"/api/auth/me";
            var json = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already registered"))
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            var json = JsonSerializer.Serialize(new { message = ex.Message }, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
    }

    private static async Task Login(
        HttpContext context,
        LoginCommand command,
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        IJwtTokenService jwtTokenService,
        CancellationToken cancellationToken)
    {
        // Validate command
        var validator = new LoginCommandValidator();
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
            var handler = new LoginHandler(userRepository, passwordHashingService, jwtTokenService);
            var response = await handler.Handle(command, cancellationToken);
            var json = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Invalid email or password"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
    }

    private static async Task GetCurrentUser(
        HttpContext context,
        ClaimsPrincipal user,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Retrieve user from database
        var handler = new CurrentUserHandler(userRepository);
        var response = await handler.Handle(userId, cancellationToken);

        if (response == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var json = JsonSerializer.Serialize(response, JsonDefaults.CamelCase);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json);
    }
}
