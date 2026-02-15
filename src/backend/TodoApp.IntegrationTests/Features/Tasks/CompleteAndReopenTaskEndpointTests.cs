using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Features.Auth.Register;
using TodoApp.Application.Features.Tasks.GetTasks;
using TodoApp.Domain.Enums;
using TodoApp.Infrastructure.Persistence;
using Xunit;
using TaskStatus = TodoApp.Domain.Enums.TaskStatus;

namespace TodoApp.IntegrationTests.Features.Tasks;

/// <summary>
/// Integration tests for Complete and Reopen Task endpoints.
/// </summary>
public class CompleteAndReopenTaskEndpointTests : IAsyncLifetime
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

    public CompleteAndReopenTaskEndpointTests()
    {
        var uniqueDbName = $"CompleteReopenTaskTests_{Guid.NewGuid()}";

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

        // Create first test user
        var registerRequest = new RegisterCommand
        {
            Email = "completereopentest@example.com",
            Password = "ValidPassword123",
            DisplayName = "Complete Reopen Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        if (!registerResponse.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Register failed: {registerResponse.StatusCode} - {errorContent}");
        }
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        _authToken = registerResult!.Token;

        var user = await _dbContext.Users.FirstAsync(u => u.Email == "completereopentest@example.com");
        _userId = user.Id;

        // Create second test user (for authorization test)
        var registerRequest2 = new RegisterCommand
        {
            Email = "otheruserforcompletion@example.com",
            Password = "ValidPassword123",
            DisplayName = "Other User For Completion"
        };

        var registerResponse2 = await _client.PostAsJsonAsync("/api/auth/register", registerRequest2);
        if (!registerResponse2.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse2.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Register failed: {registerResponse2.StatusCode} - {errorContent}");
        }
        var registerResult2 = await registerResponse2.Content.ReadFromJsonAsync<RegisterResponse>();
        _otherUserToken = registerResult2!.Token;

        var otherUser = await _dbContext.Users.FirstAsync(u => u.Email == "otheruserforcompletion@example.com");
        _otherUserId = otherUser.Id;

        // Create test task
        var task = Domain.Entities.TodoTask.Create(
            _userId,
            "Task to Complete",
            "This task will be marked complete",
            SystemList.Inbox,
            Priority.P2,
            null,
            DateTime.UtcNow.AddDays(3)
        );
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();
        _taskId = task.Id;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _scope.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CompleteTask_WithValidId_Returns200AndMarksTaskDone()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Act
        var response = await _client.PatchAsync($"/api/tasks/{_taskId}/complete", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Equal(_taskId, task.Id);
        Assert.Equal(TaskStatus.Done, task.Status);
        Assert.True(task.IsArchived);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public async Task CompleteTask_IsIdempotent()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Act - Complete task twice
        var response1 = await _client.PatchAsync($"/api/tasks/{_taskId}/complete", null);
        var task1 = await response1.Content.ReadFromJsonAsync<TaskItemDto>();

        var response2 = await _client.PatchAsync($"/api/tasks/{_taskId}/complete", null);
        var task2 = await response2.Content.ReadFromJsonAsync<TaskItemDto>();

        // Assert - Both should succeed with same status
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.Equal(TaskStatus.Done, task1!.Status);
        Assert.Equal(TaskStatus.Done, task2!.Status);
        Assert.True(task1.IsArchived);
        Assert.True(task2.IsArchived);
    }

    [Fact]
    public async Task CompleteTask_WithNonExistentId_Returns404()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.PatchAsync($"/api/tasks/{nonExistentId}/complete", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteTask_WithAnotherUsersTask_Returns404()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_otherUserToken}");

        // Act
        var response = await _client.PatchAsync($"/api/tasks/{_taskId}/complete", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteTask_WithInvalidGuid_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Act
        var response = await _client.PatchAsync("/api/tasks/invalid-guid/complete", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("GUID", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteTask_WithoutAuthentication_Returns401()
    {
        // Act
        var response = await _client.PatchAsync($"/api/tasks/{_taskId}/complete", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReopenTask_AfterCompletion_Returns200AndRestoresOpenStatus()
    {
        // Arrange - Complete task first
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        await _client.PatchAsync($"/api/tasks/{_taskId}/complete", null);

        // Act - Reopen it
        var response = await _client.PatchAsync($"/api/tasks/{_taskId}/reopen", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Equal(_taskId, task.Id);
        Assert.Equal(TaskStatus.Open, task.Status);
        Assert.False(task.IsArchived);
        Assert.Null(task.CompletedAt);
        Assert.Equal(SystemList.Inbox, task.SystemList); // Original system list retained
        Assert.Equal(0, task.SortOrder); // Task placed at top
    }

    [Fact]
    public async Task ReopenTask_IsIdempotent()
    {
        // Arrange - Complete and reopen task once
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        await _client.PatchAsync($"/api/tasks/{_taskId}/complete", null);
        await _client.PatchAsync($"/api/tasks/{_taskId}/reopen", null);

        // Act - Reopen again
        var response = await _client.PatchAsync($"/api/tasks/{_taskId}/reopen", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Equal(TaskStatus.Open, task.Status);
        Assert.False(task.IsArchived);
    }

    [Fact]
    public async Task ReopenTask_WithNonExistentId_Returns404()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.PatchAsync($"/api/tasks/{nonExistentId}/reopen", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReopenTask_WithAnotherUsersTask_Returns404()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_otherUserToken}");

        // Act
        var response = await _client.PatchAsync($"/api/tasks/{_taskId}/reopen", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReopenTask_WithInvalidGuid_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Act
        var response = await _client.PatchAsync("/api/tasks/invalid-guid/reopen", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReopenTask_WithoutAuthentication_Returns401()
    {
        // Act
        var response = await _client.PatchAsync($"/api/tasks/{_taskId}/reopen", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CompleteTask_UpdatesTimestamp()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var originalTask = await _dbContext.Tasks.FirstAsync(t => t.Id == _taskId);
        var originalUpdatedAt = originalTask.UpdatedAt;

        await Task.Delay(100); // Small delay to ensure timestamp difference

        // Act
        var response = await _client.PatchAsync($"/api/tasks/{_taskId}/complete", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.True(task.UpdatedAt > originalUpdatedAt, "UpdatedAt should be refreshed on complete");
    }

    [Fact]
    public async Task CompleteTask_SetsCompletedAtTimestamp()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var beforeComplete = DateTime.UtcNow;

        // Act
        var response = await _client.PatchAsync($"/api/tasks/{_taskId}/complete", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.NotNull(task.CompletedAt);
        Assert.True(task.CompletedAt >= beforeComplete, "CompletedAt should be set to current time or later");
    }

    [Fact]
    public async Task CompleteAndReopenFlow_PreservesTaskData()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var originalTask = await _dbContext.Tasks.FirstAsync(t => t.Id == _taskId);
        var originalName = originalTask.Name;
        var originalDescription = originalTask.Description;
        var originalPriority = originalTask.Priority;
        var originalSystemList = originalTask.SystemList;
        var originalCreatedAt = originalTask.CreatedAt;

        // Act
        await _client.PatchAsync($"/api/tasks/{_taskId}/complete", null);
        await Task.Delay(100);
        var response = await _client.PatchAsync($"/api/tasks/{_taskId}/reopen", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Equal(originalName, task.Name);
        Assert.Equal(originalDescription, task.Description);
        Assert.Equal(originalPriority, task.Priority);
        Assert.Equal(originalSystemList, task.SystemList);
        Assert.Equal(originalCreatedAt, task.CreatedAt);
    }
}
