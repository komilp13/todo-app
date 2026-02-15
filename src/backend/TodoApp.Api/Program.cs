using System.Text.Json.Serialization;
using TodoApp.Api.Endpoints;
using TodoApp.Api.Extensions;
using TodoApp.Api.Middleware;
using TodoApp.Infrastructure.Persistence;
using TodoApp.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization options (for both request and response deserialization/serialization)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
});

// Add services to the container
builder.Services
    .AddSwaggerConfiguration()
    .AddCorsConfiguration(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Seed development data (skip if database is not available)
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await ApplicationDbContextSeeder.SeedAsync(dbContext);
        }
    }
    catch (Exception ex)
    {
        // Log and continue - database might not be available in test environments
        System.Diagnostics.Debug.WriteLine($"Seed data skipped: {ex.Message}");
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapTaskEndpoints();
app.MapProjectEndpoints();

app.Run();

// Make Program public for WebApplicationFactory in integration tests
public partial class Program { }
