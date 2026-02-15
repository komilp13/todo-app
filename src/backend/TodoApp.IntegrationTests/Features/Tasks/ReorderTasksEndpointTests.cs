using TodoApp.IntegrationTests.Base;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Features.Auth.Register;
using TodoApp.Application.Features.Tasks.CreateTask;
using TodoApp.Application.Features.Tasks.ReorderTasks;
using TodoApp.Domain.Enums;
using TodoApp.Infrastructure.Persistence;
using Xunit;

namespace TodoApp.IntegrationTests.Features.Tasks;

/// <summary>
/// Integration tests for the Reorder Tasks endpoint.
/// </summary>
public class ReorderTasksEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private string? _authToken;
    private Guid _userId;
    private List<Guid> _taskIds = new();

    public ReorderTasksEndpointTests()
    {
        var uniqueDbName = $"ReorderTasksTests_{Guid.NewGuid()}";

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
            Email = "reordertest@example.com",
            Password = "ValidPassword123",
            DisplayName = "Reorder Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(registerResult);
        _authToken = registerResult.Token;

        // Extract userId from database
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "reordertest@example.com");
        Assert.NotNull(user);
        _userId = user.Id;

        // Create test tasks in Inbox
        for (int i = 0; i < 3; i++)
        {
            var taskRequest = new CreateTaskCommand
            {
                Name = $"Task {i + 1}",
                Description = null,
                Priority = Priority.P4,
                SystemList = SystemList.Inbox,
                DueDate = null,
                ProjectId = null,
                UserId = _userId
            };

            var httpRequest = CreateAuthenticatedRequest(
                HttpMethod.Post,
                "/api/tasks",
                JsonContent.Create(taskRequest));

            var response = await _client.SendAsync(httpRequest);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var taskResult = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);
            Assert.NotNull(taskResult);
            _taskIds.Add(taskResult.Id);
        }
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        _scope.Dispose();
        _factory.Dispose();
    }

    private HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string uri,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
        if (content != null)
            request.Content = content;
        return request;
    }

    [Fact]
    public async Task ReorderTasks_WithValidCommand_Returns200OK()
    {
        // Arrange
        // Reverse the task order
        var reorderedTaskIds = new[] { _taskIds[2], _taskIds[1], _taskIds[0] };

        var command = new ReorderTasksCommand
        {
            TaskIds = reorderedTaskIds,
            SystemList = SystemList.Inbox,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Patch,
            "/api/tasks/reorder",
            JsonContent.Create(command));

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ReorderTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.NotNull(result.ReorderedTasks);
        Assert.Equal(3, result.ReorderedTasks.Length);

        // Verify sort orders match the new order
        Assert.Equal(_taskIds[2], result.ReorderedTasks[0].Id);
        Assert.Equal(0, result.ReorderedTasks[0].SortOrder);

        Assert.Equal(_taskIds[1], result.ReorderedTasks[1].Id);
        Assert.Equal(1, result.ReorderedTasks[1].SortOrder);

        Assert.Equal(_taskIds[0], result.ReorderedTasks[2].Id);
        Assert.Equal(2, result.ReorderedTasks[2].SortOrder);

        // Verify in database
        var tasksFromDb = await _dbContext.Tasks
            .Where(t => t.UserId == _userId && t.SystemList == SystemList.Inbox)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        Assert.Equal(3, tasksFromDb.Count);
        Assert.Equal(_taskIds[2], tasksFromDb[0].Id);
        Assert.Equal(_taskIds[1], tasksFromDb[1].Id);
        Assert.Equal(_taskIds[0], tasksFromDb[2].Id);
    }

    [Fact]
    public async Task ReorderTasks_WithUnauthorizedUser_Returns401()
    {
        // Arrange
        var command = new ReorderTasksCommand
        {
            TaskIds = _taskIds.ToArray(),
            SystemList = SystemList.Inbox
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Patch, "/api/tasks/reorder");
        httpRequest.Content = JsonContent.Create(command);
        // No auth token

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReorderTasks_WithEmptyTaskIds_Returns400BadRequest()
    {
        // Arrange
        var command = new ReorderTasksCommand
        {
            TaskIds = Array.Empty<Guid>(),
            SystemList = SystemList.Inbox,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Patch,
            "/api/tasks/reorder",
            JsonContent.Create(command));

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReorderTasks_WithNonexistentTaskId_Returns400BadRequest()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid();
        var command = new ReorderTasksCommand
        {
            TaskIds = new[] { nonexistentId, _taskIds[1], _taskIds[2] },
            SystemList = SystemList.Inbox,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Patch,
            "/api/tasks/reorder",
            JsonContent.Create(command));

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Tasks not found", content);
    }

    [Fact]
    public async Task ReorderTasks_WithTaskFromDifferentSystemList_Returns400BadRequest()
    {
        // Arrange
        // Create a task in Next list
        var nextTaskRequest = new CreateTaskCommand
        {
            Name = "Task in Next",
            Description = null,
            Priority = Priority.P4,
            SystemList = SystemList.Next,
            DueDate = null,
            ProjectId = null,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/tasks",
            JsonContent.Create(nextTaskRequest));

        var createResponse = await _client.SendAsync(httpRequest);
        var nextTask = await createResponse.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(nextTask);

        // Try to reorder with a mix of Inbox and Next tasks
        var command = new ReorderTasksCommand
        {
            TaskIds = new[] { _taskIds[0], nextTask.Id },
            SystemList = SystemList.Inbox,
            UserId = _userId
        };

        var reorderRequest = CreateAuthenticatedRequest(
            HttpMethod.Patch,
            "/api/tasks/reorder",
            JsonContent.Create(command));

        // Act
        var response = await _client.SendAsync(reorderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("do not belong to the Inbox system list", content);
    }

    [Fact]
    public async Task ReorderTasks_WithTaskFromAnotherUser_Returns400BadRequest()
    {
        // Arrange
        // Create another user
        var anotherUserRequest = new RegisterCommand
        {
            Email = "anotheruser@example.com",
            Password = "ValidPassword123",
            DisplayName = "Another User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", anotherUserRequest);
        var anotherUserResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        var anotherUserId = (await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "anotheruser@example.com"))?.Id ?? Guid.Empty;

        // Create a task for the other user
        var otherUserTaskRequest = new CreateTaskCommand
        {
            Name = "Other User's Task",
            Description = null,
            Priority = Priority.P4,
            SystemList = SystemList.Inbox,
            DueDate = null,
            ProjectId = null,
            UserId = anotherUserId
        };

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/tasks");
        createRequest.Content = JsonContent.Create(otherUserTaskRequest);
        createRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", anotherUserResult?.Token);

        var createResponse = await _client.SendAsync(createRequest);
        var otherUserTask = await createResponse.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(otherUserTask);

        // Try to reorder current user's tasks with other user's task
        var command = new ReorderTasksCommand
        {
            TaskIds = new[] { _taskIds[0], otherUserTask.Id },
            SystemList = SystemList.Inbox,
            UserId = _userId
        };

        var reorderRequest = CreateAuthenticatedRequest(
            HttpMethod.Patch,
            "/api/tasks/reorder",
            JsonContent.Create(command));

        // Act
        var response = await _client.SendAsync(reorderRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("do not belong to the authenticated user", content);
    }
}
