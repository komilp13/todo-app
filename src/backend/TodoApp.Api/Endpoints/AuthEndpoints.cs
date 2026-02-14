using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithOpenApi()
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithOpenApi()
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithOpenApi()
            .RequireAuthorization()
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Register(
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

            return Results.BadRequest(new { errors });
        }

        try
        {
            var handler = new RegisterHandler(userRepository, passwordHashingService, jwtTokenService);
            var response = await handler.Handle(command, cancellationToken);
            return Results.Created($"/api/auth/me", response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already registered"))
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }

    private static async Task<IResult> Login(
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

            return Results.BadRequest(new { errors });
        }

        try
        {
            var handler = new LoginHandler(userRepository, passwordHashingService, jwtTokenService);
            var response = await handler.Handle(command, cancellationToken);
            return Results.Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Invalid email or password"))
        {
            return Results.Unauthorized();
        }
    }

    private static async Task<IResult> GetCurrentUser(
        ClaimsPrincipal user,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        // Extract user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        // Retrieve user from database
        var handler = new CurrentUserHandler(userRepository);
        var response = await handler.Handle(userId, cancellationToken);

        if (response == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(response);
    }
}
