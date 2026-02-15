using TodoApp.IntegrationTests.Base;
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
/// Integration tests for the Get Tasks endpoint with filtering.
/// </summary>
public class GetTasksEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private string? _authToken;
    private Guid _userId;
    private Guid _projectId;
    private Guid _labelId1;
    private Guid _labelId2;

    public GetTasksEndpointTests()
    {
        var uniqueDbName = $"GetTasksTests_{Guid.NewGuid()}";

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

        // Create test user
        var registerRequest = new RegisterCommand
        {
            Email = "taskfilter@example.com",
            Password = "ValidPassword123",
            DisplayName = "Task Filter User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        _authToken = registerResult!.Token;

        var user = await _dbContext.Users.FirstAsync(u => u.Email == "taskfilter@example.com");
        _userId = user.Id;

        // Create test project
        var project = Domain.Entities.Project.Create(_userId, "Test Project", null, null);
        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();
        _projectId = project.Id;

        // Create test labels
        var label1 = Domain.Entities.Label.Create(_userId, "Work", "#ff0000");
        var label2 = Domain.Entities.Label.Create(_userId, "Personal", "#00ff00");
        _dbContext.Labels.AddRange(label1, label2);
        await _dbContext.SaveChangesAsync();
        _labelId1 = label1.Id;
        _labelId2 = label2.Id;

        // Create test tasks with various attributes
        var tasks = new List<Domain.Entities.TodoTask>
        {
            Domain.Entities.TodoTask.Create(_userId, "Inbox Task 1", null, SystemList.Inbox, Priority.P1, null, null),
            Domain.Entities.TodoTask.Create(_userId, "Inbox Task 2", null, SystemList.Inbox, Priority.P4, null, null),
            Domain.Entities.TodoTask.Create(_userId, "Next Task 1", null, SystemList.Next, Priority.P2, _projectId, null),
            Domain.Entities.TodoTask.Create(_userId, "Upcoming Task 1", null, SystemList.Upcoming, Priority.P3, null, DateTime.UtcNow.AddDays(5)),
            Domain.Entities.TodoTask.Create(_userId, "Someday Task 1", null, SystemList.Someday, Priority.P4, null, null),
        };

        _dbContext.Tasks.AddRange(tasks);
        await _dbContext.SaveChangesAsync();

        // Add labels
        var taskLabel1 = Domain.Entities.TaskLabel.Create(tasks[0].Id, _labelId1);
        var taskLabel2 = Domain.Entities.TaskLabel.Create(tasks[2].Id, _labelId1);
        var taskLabel3 = Domain.Entities.TaskLabel.Create(tasks[2].Id, _labelId2);
        _dbContext.TaskLabels.AddRange(taskLabel1, taskLabel2, taskLabel3);

        // Create a completed task
        var completedTask = Domain.Entities.TodoTask.Create(_userId, "Completed Task", null, SystemList.Inbox, Priority.P1, null, null);
        // Use reflection to set completion status (private properties)
        completedTask.GetType().GetProperty("Status")!.SetValue(completedTask, Domain.Enums.TaskStatus.Done);
        completedTask.GetType().GetProperty("IsArchived")!.SetValue(completedTask, true);
        completedTask.GetType().GetProperty("CompletedAt")!.SetValue(completedTask, DateTime.UtcNow);
        _dbContext.Tasks.Add(completedTask);

        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        _scope.Dispose();
        _factory.Dispose();
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string uri)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
        return request;
    }

    [Fact]
    public async Task GetTasks_WithoutAuth_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/tasks");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_NoFilter_ReturnsAllOpenTasks()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal(5, result.Tasks.Length); // 5 open tasks
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task GetTasks_WithSystemListFilter_ReturnsOnlyList()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?systemList=Inbox");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result.Tasks.Length);
        Assert.All(result.Tasks, t => Assert.Equal(SystemList.Inbox, t.SystemList));
    }

    [Fact]
    public async Task GetTasks_WithProjectFilter_ReturnsProjectTasks()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/tasks?projectId={_projectId}");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Single(result.Tasks);
        Assert.Equal(_projectId, result.Tasks[0].ProjectId);
    }

    [Fact]
    public async Task GetTasks_WithLabelFilter_ReturnsLabeledTasks()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/tasks?labelId={_labelId1}");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result.Tasks.Length);
        Assert.All(result.Tasks, t => Assert.Contains(t.Labels, l => l.Id == _labelId1));
    }

    [Fact]
    public async Task GetTasks_WithStatusDone_ReturnsCompletedTasks()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?status=Done");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Single(result.Tasks);
        Assert.True(result.Tasks[0].IsArchived);
    }

    [Fact]
    public async Task GetTasks_WithStatusAll_ReturnsAllTasks()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?status=All");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal(6, result.Tasks.Length);
    }

    [Fact]
    public async Task GetTasks_WithArchivedFilter_ReturnsArchivedTasks()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?archived=true");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Single(result.Tasks);
        Assert.True(result.Tasks[0].IsArchived);
    }

    [Fact]
    public async Task GetTasks_WithMultipleFilters_ReturnsCombinedResults()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get,
            $"/api/tasks?systemList=Next&projectId={_projectId}");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Single(result.Tasks);
        Assert.Equal(SystemList.Next, result.Tasks[0].SystemList);
        Assert.Equal(_projectId, result.Tasks[0].ProjectId);
    }

    [Fact]
    public async Task GetTasks_IncludesProjectNameInResponse()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/tasks?projectId={_projectId}");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal("Test Project", result.Tasks[0].ProjectName);
    }

    [Fact]
    public async Task GetTasks_IncludesLabelInformation()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/tasks?labelId={_labelId1}");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.All(result.Tasks, t => Assert.NotEmpty(t.Labels));
    }

    [Fact]
    public async Task GetTasks_InvalidSystemList_Returns400BadRequest()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?systemList=InvalidList");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_InvalidStatus_Returns400BadRequest()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?status=Invalid");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_EmptyResult_ReturnsZeroTasks()
    {
        // Arrange
        var nonExistentProjectId = Guid.NewGuid();
        var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/tasks?projectId={nonExistentProjectId}");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Empty(result.Tasks);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetTasks_OrdersTasksBySortOrder()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?systemList=Inbox");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);

        // Verify tasks are ordered by SortOrder
        for (int i = 1; i < result.Tasks.Length; i++)
        {
            Assert.True(result.Tasks[i - 1].SortOrder <= result.Tasks[i].SortOrder);
        }
    }

    [Fact]
    public async Task GetTasks_ArchivedTasksOrderedByCompletedAt()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?status=Done");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.All(result.Tasks, t => Assert.NotNull(t.CompletedAt));
    }

    [Fact]
    public async Task GetTasks_OnlyReturnsAuthenticatedUserTasks()
    {
        // Arrange
        var otherUserRequest = new RegisterCommand
        {
            Email = "otheruser@example.com",
            Password = "ValidPassword123",
            DisplayName = "Other User"
        };

        var otherUserResponse = await _client.PostAsJsonAsync("/api/auth/register", otherUserRequest);
        var otherUserResult = await otherUserResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        var otherUserToken = otherUserResult!.Token;

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/tasks");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", otherUserToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Empty(result.Tasks); // Other user has no tasks
    }

    [Fact]
    public async Task GetTasks_UpcomingView_ReturnsTasksWithinNext14Days()
    {
        // Arrange
        // Create tasks with various due dates
        var tasks = new List<Domain.Entities.TodoTask>
        {
            Domain.Entities.TodoTask.Create(_userId, "Due Today", null, SystemList.Inbox, Priority.P1, null, DateTime.UtcNow),
            Domain.Entities.TodoTask.Create(_userId, "Due in 7 days", null, SystemList.Next, Priority.P2, null, DateTime.UtcNow.AddDays(7)),
            Domain.Entities.TodoTask.Create(_userId, "Due in 13 days", null, SystemList.Someday, Priority.P3, null, DateTime.UtcNow.AddDays(13)),
            Domain.Entities.TodoTask.Create(_userId, "Due in 15 days", null, SystemList.Inbox, Priority.P4, null, DateTime.UtcNow.AddDays(15)), // Should NOT appear
            Domain.Entities.TodoTask.Create(_userId, "No due date", null, SystemList.Inbox, Priority.P1, null, null) // Should NOT appear
        };
        _dbContext.Tasks.AddRange(tasks);
        await _dbContext.SaveChangesAsync();

        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?view=upcoming");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);

        // Should include the existing Upcoming task + 3 new tasks with dates within 14 days
        Assert.Equal(4, result.Tasks.Length);
        Assert.All(result.Tasks.Where(t => t.DueDate.HasValue), t =>
        {
            var daysDiff = (t.DueDate!.Value.Date - DateTime.UtcNow.Date).Days;
            Assert.True(daysDiff <= 14, $"Task {t.Name} has due date {daysDiff} days away");
        });
    }

    [Fact]
    public async Task GetTasks_UpcomingView_IncludesExplicitUpcomingListTasks()
    {
        // Arrange
        // Create a task in Upcoming list with no due date
        var upcomingTaskNoDueDate = Domain.Entities.TodoTask.Create(_userId, "Upcoming No Date", null, SystemList.Upcoming, Priority.P2, null, null);
        _dbContext.Tasks.Add(upcomingTaskNoDueDate);
        await _dbContext.SaveChangesAsync();

        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?view=upcoming");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);

        // Should include task with SystemList = Upcoming even without due date
        var upcomingTask = result.Tasks.FirstOrDefault(t => t.Name == "Upcoming No Date");
        Assert.NotNull(upcomingTask);
        Assert.Equal(SystemList.Upcoming, upcomingTask.SystemList);
        Assert.Null(upcomingTask.DueDate);
    }

    [Fact]
    public async Task GetTasks_UpcomingView_IncludesOverdueTasks()
    {
        // Arrange
        var overdueTasks = new List<Domain.Entities.TodoTask>
        {
            Domain.Entities.TodoTask.Create(_userId, "Overdue 1 day", null, SystemList.Inbox, Priority.P1, null, DateTime.UtcNow.AddDays(-1)),
            Domain.Entities.TodoTask.Create(_userId, "Overdue 5 days", null, SystemList.Next, Priority.P2, null, DateTime.UtcNow.AddDays(-5))
        };
        _dbContext.Tasks.AddRange(overdueTasks);
        await _dbContext.SaveChangesAsync();

        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?view=upcoming");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);

        // Should include overdue tasks
        var overdueResults = result.Tasks.Where(t =>
            t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow).ToList();
        Assert.True(overdueResults.Count >= 2);
    }

    [Fact]
    public async Task GetTasks_UpcomingView_SortsOverdueFirst()
    {
        // Arrange
        var tasks = new List<Domain.Entities.TodoTask>
        {
            Domain.Entities.TodoTask.Create(_userId, "Future 1", null, SystemList.Inbox, Priority.P1, null, DateTime.UtcNow.AddDays(3)),
            Domain.Entities.TodoTask.Create(_userId, "Overdue oldest", null, SystemList.Inbox, Priority.P1, null, DateTime.UtcNow.AddDays(-10)),
            Domain.Entities.TodoTask.Create(_userId, "Overdue recent", null, SystemList.Inbox, Priority.P1, null, DateTime.UtcNow.AddDays(-2)),
            Domain.Entities.TodoTask.Create(_userId, "Future 2", null, SystemList.Inbox, Priority.P1, null, DateTime.UtcNow.AddDays(7))
        };
        _dbContext.Tasks.AddRange(tasks);
        await _dbContext.SaveChangesAsync();

        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?view=upcoming");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);

        // Find overdue and future tasks
        var tasksWithDates = result.Tasks.Where(t => t.DueDate.HasValue).ToList();
        var now = DateTime.UtcNow;
        var overdueTasks = tasksWithDates.Where(t => t.DueDate!.Value < now).ToList();
        var futureTasks = tasksWithDates.Where(t => t.DueDate!.Value >= now).ToList();

        // Verify overdue tasks come first
        if (overdueTasks.Any() && futureTasks.Any())
        {
            var lastOverdueIndex = Array.LastIndexOf(result.Tasks, overdueTasks.Last());
            var firstFutureIndex = Array.IndexOf(result.Tasks, futureTasks.First());
            Assert.True(lastOverdueIndex < firstFutureIndex, "Overdue tasks should come before future tasks");
        }

        // Verify overdue tasks are sorted oldest first
        for (int i = 1; i < overdueTasks.Count; i++)
        {
            Assert.True(overdueTasks[i - 1].DueDate <= overdueTasks[i].DueDate,
                "Overdue tasks should be sorted oldest first");
        }

        // Verify future tasks are sorted by date ascending
        for (int i = 1; i < futureTasks.Count; i++)
        {
            Assert.True(futureTasks[i - 1].DueDate <= futureTasks[i].DueDate,
                "Future tasks should be sorted by due date ascending");
        }
    }

    [Fact]
    public async Task GetTasks_UpcomingView_ExcludesArchivedTasks()
    {
        // Arrange
        var archivedTask = Domain.Entities.TodoTask.Create(_userId, "Archived Upcoming", null, SystemList.Upcoming, Priority.P1, null, DateTime.UtcNow.AddDays(3));
        archivedTask.GetType().GetProperty("Status")!.SetValue(archivedTask, Domain.Enums.TaskStatus.Done);
        archivedTask.GetType().GetProperty("IsArchived")!.SetValue(archivedTask, true);
        _dbContext.Tasks.Add(archivedTask);
        await _dbContext.SaveChangesAsync();

        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?view=upcoming");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);

        // Should not include archived tasks
        Assert.DoesNotContain(result.Tasks, t => t.Name == "Archived Upcoming");
        Assert.All(result.Tasks, t => Assert.False(t.IsArchived));
    }

    [Fact]
    public async Task GetTasks_UpcomingView_IncludesSystemListValue()
    {
        // Arrange
        var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/tasks?view=upcoming");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetTasksResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);

        // All tasks should have their original SystemList value preserved
        Assert.All(result.Tasks, t =>
        {
            Assert.True(
                t.SystemList == SystemList.Inbox ||
                t.SystemList == SystemList.Next ||
                t.SystemList == SystemList.Upcoming ||
                t.SystemList == SystemList.Someday,
                "Task should have a valid SystemList value");
        });
    }
}
