using TodoApp.IntegrationTests.Base;
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

namespace TodoApp.IntegrationTests.Features.Tasks;

/// <summary>
/// Integration tests for Delete Task endpoint.
/// </summary>
public class DeleteTaskEndpointTests : IAsyncLifetime
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
    private Guid _labelId;

    public DeleteTaskEndpointTests()
    {
        // Use unique database name for each test instance to avoid shared state
        var uniqueDbName = $"DeleteTaskTests_{Guid.NewGuid()}";

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
            Email = "deletetask@example.com",
            Password = "ValidPassword123",
            DisplayName = "Delete Task User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        if (!registerResponse.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Register failed: {registerResponse.StatusCode} - {errorContent}");
        }
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        _authToken = registerResult!.Token;

        var user = await _dbContext.Users.FirstAsync(u => u.Email == "deletetask@example.com");
        _userId = user.Id;

        // Create second test user (for authorization test)
        var registerRequest2 = new RegisterCommand
        {
            Email = "otheruserfordeletion@example.com",
            Password = "ValidPassword123",
            DisplayName = "Other User For Deletion"
        };

        var registerResponse2 = await _client.PostAsJsonAsync("/api/auth/register", registerRequest2);
        if (!registerResponse2.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse2.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Register failed: {registerResponse2.StatusCode} - {errorContent}");
        }
        var registerResult2 = await registerResponse2.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        _otherUserToken = registerResult2!.Token;

        var otherUser = await _dbContext.Users.FirstAsync(u => u.Email == "otheruserfordeletion@example.com");
        _otherUserId = otherUser.Id;

        // Create test label
        var label = Domain.Entities.Label.Create(_userId, "TestLabel", "#ff0000");
        _dbContext.Labels.Add(label);
        await _dbContext.SaveChangesAsync();
        _labelId = label.Id;

        // Create test task
        var task = Domain.Entities.TodoTask.Create(
            _userId,
            "Task to Delete",
            "This task will be deleted",
            SystemList.Inbox,
            Priority.P2,
            null,
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
    public async Task DeleteTask_WithValidId_Returns204NoContent()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Act
        var response = await _client.DeleteAsync($"/api/tasks/{_taskId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(response.Content.Headers.ContentLength == 0 ? "" : "has content");

        // Verify task is deleted from database
        var deletedTask = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == _taskId);
        Assert.Null(deletedTask);
    }

    [Fact]
    public async Task DeleteTask_CascadeDeletesTaskLabels()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Verify task label exists before deletion
        var labelsBefore = await _dbContext.TaskLabels.Where(tl => tl.TaskId == _taskId).ToListAsync();
        Assert.Single(labelsBefore);

        // Act
        var response = await _client.DeleteAsync($"/api/tasks/{_taskId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify task labels are deleted (clear context cache first since another context modified the database)
        _dbContext.ChangeTracker.Clear();
        var labelsAfter = await _dbContext.TaskLabels.Where(tl => tl.TaskId == _taskId).ToListAsync();
        Assert.Empty(labelsAfter);

        // Verify the label itself still exists (only the association is deleted)
        var label = await _dbContext.Labels.FirstOrDefaultAsync(l => l.Id == _labelId);
        Assert.NotNull(label);
    }

    [Fact]
    public async Task DeleteTask_SubsequentGetReturns404()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Act - Delete task
        var deleteResponse = await _client.DeleteAsync($"/api/tasks/{_taskId}");

        // Try to get deleted task
        var getResponse = await _client.GetAsync($"/api/tasks/{_taskId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_WithNonExistentId_Returns404()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/tasks/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_WithAnotherUsersTask_Returns404()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_otherUserToken}");

        // Act
        var response = await _client.DeleteAsync($"/api/tasks/{_taskId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify task still exists (not deleted by other user)
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == _taskId);
        Assert.NotNull(task);
    }

    [Fact]
    public async Task DeleteTask_WithInvalidGuid_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Act
        var response = await _client.DeleteAsync("/api/tasks/invalid-guid");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("GUID", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteTask_WithoutAuthentication_Returns401()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/tasks/{_taskId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_IsIdempotent()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Act - Delete task twice
        var response1 = await _client.DeleteAsync($"/api/tasks/{_taskId}");
        var response2 = await _client.DeleteAsync($"/api/tasks/{_taskId}");

        // Assert - First succeeds, second returns 404 (already deleted)
        Assert.Equal(HttpStatusCode.NoContent, response1.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_DoesNotAffectOtherUsersTasks()
    {
        // Arrange - Create a task for the other user
        var otherUserTask = Domain.Entities.TodoTask.Create(
            _otherUserId,
            "Other User's Task",
            "Should not be deleted",
            SystemList.Inbox,
            Priority.P3,
            null,
            null
        );
        _dbContext.Tasks.Add(otherUserTask);
        await _dbContext.SaveChangesAsync();
        var otherUserTaskId = otherUserTask.Id;

        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Act - Delete the first user's task
        var response = await _client.DeleteAsync($"/api/tasks/{_taskId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify only the first user's task is deleted, not the other user's
        var deletedTask = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == _taskId);
        Assert.Null(deletedTask);

        var otherUserTaskStillExists = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == otherUserTaskId);
        Assert.NotNull(otherUserTaskStillExists);
    }

    [Fact]
    public async Task DeleteTask_RemovesTaskFromQueries()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}");

        // Verify task appears in query before deletion
        var response1 = await _client.GetAsync("/api/tasks?systemList=Inbox");
        var tasks1 = await response1.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        var taskExistsBefore = tasks1?.Tasks.Any(t => t.Id == _taskId) ?? false;
        Assert.True(taskExistsBefore);

        // Act - Delete task
        await _client.DeleteAsync($"/api/tasks/{_taskId}");

        // Assert - Task no longer appears in query
        var response2 = await _client.GetAsync("/api/tasks?systemList=Inbox");
        var tasks2 = await response2.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        var taskExistsAfter = tasks2?.Tasks.Any(t => t.Id == _taskId) ?? false;
        Assert.False(taskExistsAfter);
    }
}
