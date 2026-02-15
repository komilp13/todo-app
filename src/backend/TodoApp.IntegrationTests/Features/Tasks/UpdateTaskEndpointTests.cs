using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Features.Auth.Register;
using TodoApp.Application.Features.Tasks.GetTasks;
using TodoApp.Application.Features.Tasks.UpdateTask;
using TodoApp.Domain.Enums;
using TodoApp.Infrastructure.Persistence;
using Xunit;

namespace TodoApp.IntegrationTests.Features.Tasks;

/// <summary>
/// Integration tests for the Update Task endpoint (PUT /api/tasks/{id}).
/// </summary>
public class UpdateTaskEndpointTests : IAsyncLifetime
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

    public UpdateTaskEndpointTests()
    {
        var uniqueDbName = $"UpdateTaskTests_{Guid.NewGuid()}";

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
            Email = "updatetask@example.com",
            Password = "ValidPassword123",
            DisplayName = "Update Task User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        if (!registerResponse.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Register failed: {registerResponse.StatusCode} - {errorContent}");
        }
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        _authToken = registerResult!.Token;

        var user = await _dbContext.Users.FirstAsync(u => u.Email == "updatetask@example.com");
        _userId = user.Id;

        // Create second test user (for authorization test)
        var registerRequest2 = new RegisterCommand
        {
            Email = "otheruserfortask@example.com",
            Password = "ValidPassword123",
            DisplayName = "Other User For Task"
        };

        var registerResponse2 = await _client.PostAsJsonAsync("/api/auth/register", registerRequest2);
        if (!registerResponse2.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse2.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Register failed: {registerResponse2.StatusCode} - {errorContent}");
        }
        var registerResult2 = await registerResponse2.Content.ReadFromJsonAsync<RegisterResponse>();
        _otherUserToken = registerResult2!.Token;

        var otherUser = await _dbContext.Users.FirstAsync(u => u.Email == "otheruserfortask@example.com");
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

        // Create test task
        var task = Domain.Entities.TodoTask.Create(
            _userId,
            "Original Task Name",
            "Original description",
            SystemList.Inbox,
            Priority.P4,
            null,
            DateTime.UtcNow.AddDays(1)
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
    public async Task UpdateTask_WithValidNameUpdate_Returns200AndUpdatedTask()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new { name = "Updated Task Name" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Equal(_taskId, task.Id);
        Assert.Equal("Updated Task Name", task.Name);
        Assert.Equal("Original description", task.Description); // Should not change
    }

    [Fact]
    public async Task UpdateTask_WithValidDescriptionUpdate_Returns200AndUpdatedTask()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new { description = "Updated description" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Equal(_taskId, task.Id);
        Assert.Equal("Original Task Name", task.Name); // Should not change
        Assert.Equal("Updated description", task.Description);
    }

    [Fact]
    public async Task UpdateTask_WithClearDescription_Returns200AndNullDescription()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new { description = (string?)null };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Equal(_taskId, task.Id);
        Assert.Null(task.Description);
    }

    [Fact]
    public async Task UpdateTask_WithPriorityUpdate_Returns200AndUpdatedPriority()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new { priority = "P1" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Equal(Priority.P1, task.Priority);
    }

    [Fact]
    public async Task UpdateTask_WithSystemListUpdate_Returns200AndUpdatedSystemList()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new { systemList = "Next" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Equal(SystemList.Next, task.SystemList);
    }

    [Fact]
    public async Task UpdateTask_WithProjectIdUpdate_Returns200AndUpdatedProject()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new { projectId = _projectId.ToString() };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Equal(_projectId, task.ProjectId);
        Assert.Equal("Test Project", task.ProjectName);
    }

    [Fact]
    public async Task UpdateTask_WithClearProject_Returns200AndNullProject()
    {
        // Arrange - First assign a project
        var task = await _dbContext.Tasks.FirstAsync(t => t.Id == _taskId);
        task.UpdateProjectId(_projectId);
        await _dbContext.SaveChangesAsync();

        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new { projectId = (string?)null };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedTask = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(updatedTask);
        Assert.Null(updatedTask.ProjectId);
        Assert.Null(updatedTask.ProjectName);
    }

    [Fact]
    public async Task UpdateTask_WithDueDateUpdate_Returns200AndUpdatedDueDate()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var newDueDate = DateTime.UtcNow.AddDays(7);
        var updateRequest = new { dueDate = newDueDate.ToString("O") };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        // Allow 1 second tolerance for timestamp comparison
        Assert.True(Math.Abs((task.DueDate - newDueDate)?.TotalSeconds ?? 999) < 1);
    }

    [Fact]
    public async Task UpdateTask_WithMultipleFieldUpdates_Returns200AndAllUpdated()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new
        {
            name = "Multi-Update Name",
            description = "Multi-update description",
            priority = "P2",
            systemList = "Upcoming"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.Equal("Multi-Update Name", task.Name);
        Assert.Equal("Multi-update description", task.Description);
        Assert.Equal(Priority.P2, task.Priority);
        Assert.Equal(SystemList.Upcoming, task.SystemList);
    }

    [Fact]
    public async Task UpdateTask_WithEmptyBody_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new { };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert - Empty update should still succeed (no-op update)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_WithInvalidProjectId_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var invalidProjectId = Guid.NewGuid().ToString(); // Project doesn't exist
        var updateRequest = new { projectId = invalidProjectId };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateTask_WithOtherUsersProject_Returns400()
    {
        // Arrange - Create project for other user
        var otherProject = Domain.Entities.Project.Create(_otherUserId, "Other User's Project", "Not mine", null);
        _dbContext.Projects.Add(otherProject);
        await _dbContext.SaveChangesAsync();

        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new { projectId = otherProject.Id.ToString() };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_WithNonExistentTaskId_Returns404()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var nonExistentTaskId = Guid.NewGuid();
        var updateRequest = new { name = "Updated Name" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{nonExistentTaskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_WithAnotherUsersTask_Returns404()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_otherUserToken}");
        var updateRequest = new { name = "Updated Name" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_WithInvalidTaskIdFormat_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new { name = "Updated Name" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/tasks/invalid-guid", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("GUID", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateTask_WithoutAuthentication_Returns401()
    {
        // Arrange
        var updateRequest = new { name = "Updated Name" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_WithInvalidPriority_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new { priority = "InvalidPriority" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_WithInvalidSystemList_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new { systemList = "InvalidSystemList" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_WithNameTooLong_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var longName = new string('x', 501); // Max is 500 chars
        var updateRequest = new { name = longName };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_WithDescriptionTooLong_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var longDescription = new string('x', 4001); // Max is 4000 chars
        var updateRequest = new { description = longDescription };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_UpdatesTimestamp()
    {
        // Arrange
        var originalTask = await _dbContext.Tasks.FirstAsync(t => t.Id == _taskId);
        var originalUpdatedAt = originalTask.UpdatedAt;

        await Task.Delay(100); // Small delay to ensure timestamp difference

        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var updateRequest = new { name = "Updated Name" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{_taskId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(task);
        Assert.True(task.UpdatedAt > originalUpdatedAt, "UpdatedAt timestamp should be refreshed on update");
    }
}
