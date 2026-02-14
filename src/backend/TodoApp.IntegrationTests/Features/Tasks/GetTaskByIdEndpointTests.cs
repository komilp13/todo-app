using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Features.Auth.Register;
using TodoApp.Application.Features.Tasks.CreateTask;
using TodoApp.Application.Features.Tasks.GetTasks;
using TodoApp.Domain.Enums;
using TodoApp.Infrastructure.Persistence;
using Xunit;

namespace TodoApp.IntegrationTests.Features.Tasks;

/// <summary>
/// Integration tests for the Get Single Task endpoint (GET /api/tasks/{id}).
/// </summary>
public class GetTaskByIdEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private string? _authToken;
    private string? _otherUserToken;
    private Guid _userId;
    private Guid _otherUserId;
    private Guid _taskId;
    private Guid _projectId;
    private Guid _labelId;

    public GetTaskByIdEndpointTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.FirstOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase("GetTaskByIdTests"));
                });
            });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();

        // Create first test user
        var registerRequest = new RegisterCommand
        {
            Email = "gettaskbyid@example.com",
            Password = "ValidPassword123",
            DisplayName = "Get Task By ID User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        if (!registerResponse.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Register failed: {registerResponse.StatusCode} - {errorContent}");
        }
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        _authToken = registerResult!.Token;

        var user = await _dbContext.Users.FirstAsync(u => u.Email == "gettaskbyid@example.com");
        _userId = user.Id;

        // Create second test user (for authorization test)
        var registerRequest2 = new RegisterCommand
        {
            Email = "otheruser@example.com",
            Password = "ValidPassword123",
            DisplayName = "Other User"
        };

        var registerResponse2 = await _client.PostAsJsonAsync("/api/auth/register", registerRequest2);
        if (!registerResponse2.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse2.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Register failed: {registerResponse2.StatusCode} - {errorContent}");
        }
        var registerResult2 = await registerResponse2.Content.ReadFromJsonAsync<RegisterResponse>();
        _otherUserToken = registerResult2!.Token;

        var otherUser = await _dbContext.Users.FirstAsync(u => u.Email == "otheruser@example.com");
        _otherUserId = otherUser.Id;

        // Create test project
        var project = Domain.Entities.Project.Create(_userId, "Test Project", "A test project", null);
        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();
        _projectId = project.Id;

        // Create test label
        var label = Domain.Entities.Label.Create(_userId, "Work", "#ff0000");
        _dbContext.Labels.Add(label);
        await _dbContext.SaveChangesAsync();
        _labelId = label.Id;

        // Create test task with all attributes
        var task = Domain.Entities.TodoTask.Create(
            _userId,
            "Test Task",
            "This is a test task",
            SystemList.Inbox,
            Priority.P1,
            _projectId,
            DateTime.UtcNow.AddDays(1)
        );
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();
        _taskId = task.Id;

        // Assign label to task
        var taskLabel = Domain.Entities.TaskLabel.Create(_taskId, _labelId);
        _dbContext.TaskLabels.Add(taskLabel);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _scope.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetTaskById_WithValidId_ReturnsTaskWithFullDetails()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Act
        var response = await _client.GetAsync($"/api/tasks/{_taskId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Equal(_taskId, task.Id);
        Assert.Equal("Test Task", task.Name);
        Assert.Equal("This is a test task", task.Description);
        Assert.Equal(Priority.P1, task.Priority);
        Assert.Equal(SystemList.Inbox, task.SystemList);
        Assert.Equal(_projectId, task.ProjectId);
        Assert.Equal("Test Project", task.ProjectName);
        Assert.False(task.IsArchived);
        Assert.Single(task.Labels);
        Assert.Equal("Work", task.Labels[0].Name);
        Assert.Equal("#ff0000", task.Labels[0].Color);
    }

    [Fact]
    public async Task GetTaskById_WithNonExistentId_Returns404()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/tasks/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTaskById_WithAnotherUsersTask_Returns404()
    {
        // Arrange
        // Use other user's token
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_otherUserToken}");

        // Act
        var response = await _client.GetAsync($"/api/tasks/{_taskId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTaskById_WithInvalidGuid_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Act
        var response = await _client.GetAsync("/api/tasks/invalid-guid");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("GUID", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTaskById_WithoutAuthentication_Returns401()
    {
        // Act
        var response = await _client.GetAsync($"/api/tasks/{_taskId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTaskById_WithTaskWithoutProject_ReturnsNullProjectName()
    {
        // Arrange
        // Create task without project
        var taskWithoutProject = Domain.Entities.TodoTask.Create(
            _userId,
            "Task Without Project",
            null,
            SystemList.Next,
            Priority.P4,
            null, // No project
            null
        );
        _dbContext.Tasks.Add(taskWithoutProject);
        await _dbContext.SaveChangesAsync();

        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Act
        var response = await _client.GetAsync($"/api/tasks/{taskWithoutProject.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Null(task.ProjectId);
        Assert.Null(task.ProjectName);
    }

    [Fact]
    public async Task GetTaskById_WithTaskWithoutLabels_ReturnsEmptyLabelsArray()
    {
        // Arrange
        // Create task without labels
        var taskWithoutLabels = Domain.Entities.TodoTask.Create(
            _userId,
            "Task Without Labels",
            null,
            SystemList.Upcoming,
            Priority.P2,
            null,
            null
        );
        _dbContext.Tasks.Add(taskWithoutLabels);
        await _dbContext.SaveChangesAsync();

        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Act
        var response = await _client.GetAsync($"/api/tasks/{taskWithoutLabels.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Empty(task.Labels);
    }
}
