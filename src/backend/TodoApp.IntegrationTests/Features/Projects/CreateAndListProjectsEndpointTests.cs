using TodoApp.IntegrationTests.Base;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Features.Auth.Register;
using TodoApp.Application.Features.Projects.CreateProject;
using TodoApp.Application.Features.Projects.GetProjects;
using TodoApp.Application.Features.Tasks.CreateTask;
using TodoApp.Domain.Enums;
using TodoApp.Infrastructure.Persistence;
using Xunit;

namespace TodoApp.IntegrationTests.Features.Projects;

/// <summary>
/// Integration tests for Create Project and List Projects endpoints.
/// </summary>
public class CreateAndListProjectsEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private string? _authToken;
    private Guid _userId;

    public CreateAndListProjectsEndpointTests()
    {
        var uniqueDbName = $"ProjectTests_{Guid.NewGuid()}";

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace real database with in-memory for testing
                    var descriptor = services.FirstOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase(uniqueDbName));
                });
            });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();

        // Create a test user and get auth token
        var registerRequest = new RegisterCommand
        {
            Email = "projecttest@example.com",
            Password = "ValidPassword123",
            DisplayName = "Project Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(registerResult);
        _authToken = registerResult.Token;

        // Extract userId from database
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "projecttest@example.com");
        Assert.NotNull(user);
        _userId = user.Id;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        _scope.Dispose();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CreateProject_WithValidRequest_Returns201AndProject()
    {
        // Arrange
        var request = new CreateProjectCommand
        {
            Name = "My First Project",
            Description = "A test project",
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/projects", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var project = await response.Content.ReadFromJsonAsync<CreateProjectResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(project);
        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal("My First Project", project.Name);
        Assert.Equal("A test project", project.Description);
        Assert.NotNull(project.DueDate);
        Assert.Equal(ProjectStatus.Active, project.Status);
        Assert.Equal(0, project.SortOrder); // New projects start at 0
        Assert.True(DateTime.UtcNow.AddSeconds(-5) < project.CreatedAt && project.CreatedAt <= DateTime.UtcNow);
        Assert.True(DateTime.UtcNow.AddSeconds(-5) < project.UpdatedAt && project.UpdatedAt <= DateTime.UtcNow);

        // Verify Location header
        Assert.True(response.Headers.Location?.ToString().Contains($"/api/projects/{project.Id}"));
    }

    [Fact]
    public async Task CreateProject_WithMissingName_Returns400()
    {
        // Arrange
        var request = new CreateProjectCommand
        {
            Name = "", // Empty name
            Description = "Description"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/projects", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithNameTooLong_Returns400()
    {
        // Arrange
        var request = new CreateProjectCommand
        {
            Name = new string('a', 201), // 201 characters exceeds max of 200
            Description = "Description"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/projects", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithoutAuth_Returns401()
    {
        // Arrange
        var request = new CreateProjectCommand
        {
            Name = "Unauthorized Project"
        };

        // Act - no auth header
        var response = await _client.PostAsJsonAsync("/api/projects", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_ReturnsAllUserProjects_SortedBySortOrder()
    {
        // Arrange - Create multiple projects
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        var project1 = new CreateProjectCommand { Name = "Project Alpha" };
        var project2 = new CreateProjectCommand { Name = "Project Beta", Description = "Second project" };
        var project3 = new CreateProjectCommand { Name = "Project Gamma", DueDate = DateTime.UtcNow.AddDays(7) };

        await _client.PostAsJsonAsync("/api/projects", project1);
        await _client.PostAsJsonAsync("/api/projects", project2);
        await _client.PostAsJsonAsync("/api/projects", project3);

        // Act
        var response = await _client.GetAsync("/api/projects");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetProjectsResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Projects.Length);

        // Verify projects are sorted by sort order (all 0 for now, so insertion order)
        Assert.Contains(result.Projects, p => p.Name == "Project Alpha");
        Assert.Contains(result.Projects, p => p.Name == "Project Beta");
        Assert.Contains(result.Projects, p => p.Name == "Project Gamma");
    }

    [Fact]
    public async Task GetProjects_WithTaskStatistics_ReturnsCorrectCounts()
    {
        // Arrange - Create a project and add tasks to it
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        // Create project
        var projectRequest = new CreateProjectCommand { Name = "Project With Tasks" };
        var projectResponse = await _client.PostAsJsonAsync("/api/projects", projectRequest);
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(project);

        // Create tasks for the project
        var task1 = new CreateTaskCommand { Name = "Task 1", ProjectId = project.Id };
        var task2 = new CreateTaskCommand { Name = "Task 2", ProjectId = project.Id };
        var task3 = new CreateTaskCommand { Name = "Task 3", ProjectId = project.Id };

        var task1Response = await _client.PostAsJsonAsync("/api/tasks", task1);
        var task2Response = await _client.PostAsJsonAsync("/api/tasks", task2);
        var task3Response = await _client.PostAsJsonAsync("/api/tasks", task3);

        // Complete one task
        var task1Result = await task1Response.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(task1Result);
        await _client.PatchAsync($"/api/tasks/{task1Result.Id}/complete", null);

        // Act - Get projects
        var response = await _client.GetAsync("/api/projects");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetProjectsResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Single(result.Projects);

        var projectWithStats = result.Projects[0];
        Assert.Equal("Project With Tasks", projectWithStats.Name);
        Assert.Equal(3, projectWithStats.TotalTaskCount);
        Assert.Equal(1, projectWithStats.CompletedTaskCount);
        Assert.Equal(33, projectWithStats.CompletionPercentage); // 1/3 = 33%
    }

    [Fact]
    public async Task GetProjects_WithNoTasks_ReturnsZeroStatistics()
    {
        // Arrange - Create a project without tasks
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        var projectRequest = new CreateProjectCommand { Name = "Empty Project" };
        await _client.PostAsJsonAsync("/api/projects", projectRequest);

        // Act
        var response = await _client.GetAsync("/api/projects");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetProjectsResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Single(result.Projects);

        var project = result.Projects[0];
        Assert.Equal("Empty Project", project.Name);
        Assert.Equal(0, project.TotalTaskCount);
        Assert.Equal(0, project.CompletedTaskCount);
        Assert.Equal(0, project.CompletionPercentage);
    }

    [Fact]
    public async Task GetProjects_WithoutAuth_Returns401()
    {
        // Act - no auth header
        var response = await _client.GetAsync("/api/projects");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_OnlyReturnsUserOwnProjects()
    {
        // Arrange - Create project for first user
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
        var project1 = new CreateProjectCommand { Name = "User 1 Project" };
        await _client.PostAsJsonAsync("/api/projects", project1);

        // Create second user and their project
        var registerRequest2 = new RegisterCommand
        {
            Email = "projecttest2@example.com",
            Password = "ValidPassword123",
            DisplayName = "Project Test User 2"
        };
        var registerResponse2 = await _client.PostAsJsonAsync("/api/auth/register", registerRequest2);
        var registerResult2 = await registerResponse2.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(registerResult2);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerResult2.Token);
        var project2 = new CreateProjectCommand { Name = "User 2 Project" };
        await _client.PostAsJsonAsync("/api/projects", project2);

        // Act - Get projects for second user
        var response = await _client.GetAsync("/api/projects");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetProjectsResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Single(result.Projects); // Should only see their own project
        Assert.Equal("User 2 Project", result.Projects[0].Name);
    }
}
