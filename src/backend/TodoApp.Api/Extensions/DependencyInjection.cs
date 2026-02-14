using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TodoApp.Application.Configuration;
using TodoApp.Application.Services;
using TodoApp.Domain.Interfaces;
using TodoApp.Infrastructure.Persistence;
using TodoApp.Infrastructure.Persistence.Repositories;

namespace TodoApp.Api.Extensions;

/// <summary>
/// Extension methods for registering application services in the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers application layer services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register HTTP context accessor for extracting user claims (must be singleton)
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Register password hashing service
        services.AddScoped<IPasswordHashingService, PasswordHashingService>();

        // Register JWT token service
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Register other application services here
        // Example: services.AddScoped<IUserService, UserService>();
        return services;
    }

    /// <summary>
    /// Registers infrastructure layer services including database context.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString)
        );

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();

        // Register other infrastructure services here
        return services;
    }

    /// <summary>
    /// Configures CORS based on appsettings configuration.
    /// </summary>
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", builder =>
            {
                builder.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Configures Swagger/OpenAPI documentation.
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }

    /// <summary>
    /// Configures JWT Bearer authentication.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>();
        if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
            throw new InvalidOperationException("JWT configuration is missing or invalid");

        var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // Add Authorization service
        services.AddAuthorization();

        return services;
    }
}
