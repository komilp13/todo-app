using TodoApp.IntegrationTests.Base;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Features.Auth.Register;
using TodoApp.Application.Features.Tasks.CreateTask;
using TodoApp.Domain.Enums;
using TodoApp.Infrastructure.Persistence;
using Xunit;

namespace TodoApp.IntegrationTests.Features.Tasks;

/// <summary>
/// Integration tests for the Create Task endpoint.
/// </summary>
public class CreateTaskEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private string? _authToken;
    private Guid _userId;

    public CreateTaskEndpointTests()
    {
        var uniqueDbName = $"CreateTaskTests_{Guid.NewGuid()}";

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
            Email = "tasktest@example.com",
            Password = "ValidPassword123",
            DisplayName = "Task Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(registerResult);
        _authToken = registerResult.Token;

        // Extract userId from database
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "tasktest@example.com");
        Assert.NotNull(user);
        _userId = user.Id;
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
    public async Task CreateTask_WithValidCommand_Returns201Created()
    {
        // Arrange
        var request = new CreateTaskCommand
        {
            Name = "Buy groceries",
            Description = "Milk, eggs, bread",
            Priority = Priority.P2,
            SystemList = SystemList.Inbox,
            DueDate = null,
            ProjectId = null,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/tasks",
            JsonContent.Create(request));

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal("Buy groceries", result.Name);
        Assert.Equal("Milk, eggs, bread", result.Description);
        Assert.Equal(Priority.P2, result.Priority);
        Assert.Equal(SystemList.Inbox, result.SystemList);
        Assert.Equal(Domain.Enums.TaskStatus.Open, result.Status);
        Assert.False(result.IsArchived);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateTask_WithoutAuth_Returns401Unauthorized()
    {
        // Arrange
        var request = new CreateTaskCommand
        {
            Name = "Test Task",
            UserId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WithMissingName_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateTaskCommand
        {
            Name = "", // Empty name
            Priority = Priority.P1,
            SystemList = SystemList.Inbox,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/tasks",
            JsonContent.Create(request));

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WithTaskNameExceedingMaxLength_Returns400BadRequest()
    {
        // Arrange
        var longName = new string('a', 501); // Exceeds 500 char limit
        var request = new CreateTaskCommand
        {
            Name = longName,
            Priority = Priority.P1,
            SystemList = SystemList.Inbox,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/tasks",
            JsonContent.Create(request));

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Fact]
    public async Task CreateTask_WithFutureDate_Returns201Created()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(5).Date;
        var request = new CreateTaskCommand
        {
            Name = "Future Task",
            DueDate = futureDate,
            Priority = Priority.P1,
            SystemList = SystemList.Next,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/tasks",
            JsonContent.Create(request));

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal(futureDate, result.DueDate);
    }

    [Fact]
    public async Task CreateTask_WithPastDate_Returns400BadRequest()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-1).Date;
        var request = new CreateTaskCommand
        {
            Name = "Past Task",
            DueDate = pastDate,
            Priority = Priority.P1,
            SystemList = SystemList.Inbox,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/tasks",
            JsonContent.Create(request));

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WithValidProject_Returns201Created()
    {
        // Arrange - First create a project
        var projectName = "Test Project";
        var project = Domain.Entities.Project.Create(_userId, projectName, null, null);
        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();

        var request = new CreateTaskCommand
        {
            Name = "Task in Project",
            Priority = Priority.P1,
            SystemList = SystemList.Inbox,
            ProjectId = project.Id,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/tasks",
            JsonContent.Create(request));

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal(project.Id, result.ProjectId);
    }

    [Fact]
    public async Task CreateTask_WithInvalidProjectId_Returns404NotFound()
    {
        // Arrange
        var nonExistentProjectId = Guid.NewGuid();
        var request = new CreateTaskCommand
        {
            Name = "Task with invalid project",
            Priority = Priority.P1,
            SystemList = SystemList.Inbox,
            ProjectId = nonExistentProjectId,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/tasks",
            JsonContent.Create(request));

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WithDifferentSystemLists_PersistsCorrectly()
    {
        // Arrange
        var systemLists = new[] { SystemList.Inbox, SystemList.Next, SystemList.Upcoming, SystemList.Someday };

        // Act & Assert
        foreach (var list in systemLists)
        {
            var request = new CreateTaskCommand
            {
                Name = $"Task in {list}",
                Priority = Priority.P1,
                SystemList = list,
                UserId = _userId
            };

            var httpRequest = CreateAuthenticatedRequest(
                HttpMethod.Post,
                "/api/tasks",
                JsonContent.Create(request));

            var response = await _client.SendAsync(httpRequest);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);
            Assert.NotNull(result);
            Assert.Equal(list, result.SystemList);

            // Verify in database
            var taskInDb = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == result.Id);
            Assert.NotNull(taskInDb);
            Assert.Equal(list, taskInDb.SystemList);
        }
    }

    [Fact]
    public async Task CreateTask_CreatedTaskAppears_InUserTasksList()
    {
        // Arrange
        var request = new CreateTaskCommand
        {
            Name = "Verify in list task",
            Priority = Priority.P1,
            SystemList = SystemList.Inbox,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/tasks",
            JsonContent.Create(request));

        // Act
        var response = await _client.SendAsync(httpRequest);
        var result = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);

        // Assert - Verify task exists in database
        var taskInDb = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == result!.Id && t.UserId == _userId);

        Assert.NotNull(taskInDb);
        Assert.Equal("Verify in list task", taskInDb.Name);
        Assert.Equal(SystemList.Inbox, taskInDb.SystemList);
        Assert.Equal(Domain.Enums.TaskStatus.Open, taskInDb.Status);
    }

    [Fact]
    public async Task CreateTask_WithLongDescription_Returns201Created()
    {
        // Arrange
        var longDescription = new string('a', 4000); // Max length
        var request = new CreateTaskCommand
        {
            Name = "Long description task",
            Description = longDescription,
            Priority = Priority.P1,
            SystemList = SystemList.Inbox,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/tasks",
            JsonContent.Create(request));

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal(longDescription, result.Description);
    }

    [Fact]
    public async Task CreateTask_WithExceededDescriptionLength_Returns400BadRequest()
    {
        // Arrange
        var tooLongDescription = new string('a', 4001); // Exceeds max length
        var request = new CreateTaskCommand
        {
            Name = "Too long description task",
            Description = tooLongDescription,
            Priority = Priority.P1,
            SystemList = SystemList.Inbox,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/tasks",
            JsonContent.Create(request));

        // Act
        var response = await _client.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_ResponseLocationHeaderIsValid()
    {
        // Arrange
        var request = new CreateTaskCommand
        {
            Name = "Location header test",
            Priority = Priority.P1,
            SystemList = SystemList.Inbox,
            UserId = _userId
        };

        var httpRequest = CreateAuthenticatedRequest(
            HttpMethod.Post,
            "/api/tasks",
            JsonContent.Create(request));

        // Act
        var response = await _client.SendAsync(httpRequest);
        var result = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);

        // Assert
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/tasks/{result!.Id}", response.Headers.Location!.ToString());
    }
}
