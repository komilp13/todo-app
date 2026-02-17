using TodoApp.IntegrationTests.Base;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Features.Auth.Register;
using TodoApp.Application.Features.Labels.CreateLabel;
using TodoApp.Application.Features.Tasks.CreateTask;
using TodoApp.Application.Features.Tasks.GetTasks;
using TodoApp.Infrastructure.Persistence;
using Xunit;

namespace TodoApp.IntegrationTests.Features.Tasks;

public class TaskLabelAssignmentEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private string? _authToken;
    private Guid _userId;

    public TaskLabelAssignmentEndpointTests()
    {
        var uniqueDbName = $"TaskLabelAssignmentTests_{Guid.NewGuid()}";

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

        var registerRequest = new RegisterCommand
        {
            Email = "tasklabeltest@example.com",
            Password = "ValidPassword123",
            DisplayName = "Task Label Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(registerResult);
        _authToken = registerResult.Token;

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "tasklabeltest@example.com");
        Assert.NotNull(user);
        _userId = user.Id;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
    }

    public async Task DisposeAsync()
    {
        _scope.Dispose();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<Guid> CreateTestTask(string name = "Test Task")
    {
        var command = new CreateTaskCommand { Name = name };
        var response = await _client.PostAsJsonAsync("/api/tasks", command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        return result.Id;
    }

    private async Task<Guid> CreateTestLabel(string name = "Test Label", string? color = "#ff4040")
    {
        var command = new CreateLabelCommand { Name = name, Color = color };
        var response = await _client.PostAsJsonAsync("/api/labels", command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateLabelResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        return result.Id;
    }

    [Fact]
    public async Task AssignLabel_WithValidIds_ReturnsTaskWithLabel()
    {
        var taskId = await CreateTestTask();
        var labelId = await CreateTestLabel("Work");

        var response = await _client.PostAsync($"/api/tasks/{taskId}/labels/{labelId}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(task);
        Assert.Single(task.Labels);
        Assert.Equal(labelId, task.Labels[0].Id);
        Assert.Equal("Work", task.Labels[0].Name);
    }

    [Fact]
    public async Task AssignLabel_DuplicateAssignment_IsIdempotent()
    {
        var taskId = await CreateTestTask();
        var labelId = await CreateTestLabel("Idempotent");

        var response1 = await _client.PostAsync($"/api/tasks/{taskId}/labels/{labelId}", null);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var response2 = await _client.PostAsync($"/api/tasks/{taskId}/labels/{labelId}", null);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var task = await response2.Content.ReadFromJsonAsync<TaskItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(task);
        Assert.Single(task.Labels);
    }

    [Fact]
    public async Task AssignLabel_MultipleLabels_ReturnsAllLabels()
    {
        var taskId = await CreateTestTask();
        var labelId1 = await CreateTestLabel("Label1");
        var labelId2 = await CreateTestLabel("Label2");

        await _client.PostAsync($"/api/tasks/{taskId}/labels/{labelId1}", null);
        var response = await _client.PostAsync($"/api/tasks/{taskId}/labels/{labelId2}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(task);
        Assert.Equal(2, task.Labels.Length);
    }

    [Fact]
    public async Task AssignLabel_TaskNotFound_Returns404()
    {
        var labelId = await CreateTestLabel("Orphan");
        var fakeTaskId = Guid.NewGuid();

        var response = await _client.PostAsync($"/api/tasks/{fakeTaskId}/labels/{labelId}", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignLabel_LabelNotFound_Returns404()
    {
        var taskId = await CreateTestTask();
        var fakeLabelId = Guid.NewGuid();

        var response = await _client.PostAsync($"/api/tasks/{taskId}/labels/{fakeLabelId}", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignLabel_InvalidTaskId_Returns400()
    {
        var labelId = await CreateTestLabel("BadId");

        var response = await _client.PostAsync($"/api/tasks/not-a-guid/labels/{labelId}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignLabel_InvalidLabelId_Returns400()
    {
        var taskId = await CreateTestTask();

        var response = await _client.PostAsync($"/api/tasks/{taskId}/labels/not-a-guid", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoveLabel_WithValidIds_ReturnsTaskWithoutLabel()
    {
        var taskId = await CreateTestTask();
        var labelId = await CreateTestLabel("ToRemove");

        await _client.PostAsync($"/api/tasks/{taskId}/labels/{labelId}", null);

        var response = await _client.DeleteAsync($"/api/tasks/{taskId}/labels/{labelId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(task);
        Assert.Empty(task.Labels);
    }

    [Fact]
    public async Task RemoveLabel_NotAssigned_IsIdempotent()
    {
        var taskId = await CreateTestTask();
        var labelId = await CreateTestLabel("NeverAssigned");

        var response = await _client.DeleteAsync($"/api/tasks/{taskId}/labels/{labelId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(task);
        Assert.Empty(task.Labels);
    }

    [Fact]
    public async Task RemoveLabel_TaskNotFound_Returns404()
    {
        var labelId = await CreateTestLabel("OrphanRemove");
        var fakeTaskId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/tasks/{fakeTaskId}/labels/{labelId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemoveLabel_LabelNotFound_Returns404()
    {
        var taskId = await CreateTestTask();
        var fakeLabelId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/tasks/{taskId}/labels/{fakeLabelId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignLabel_OtherUsersTask_Returns404()
    {
        var taskId = await CreateTestTask();

        // Register a second user
        var client2 = _factory.CreateClient();
        var register2 = new RegisterCommand
        {
            Email = "otheruser@example.com",
            Password = "ValidPassword123",
            DisplayName = "Other User"
        };
        var registerResponse = await client2.PostAsJsonAsync("/api/auth/register", register2);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(registerResult);
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerResult.Token);

        // Create a label owned by user2
        var labelCommand = new CreateLabelCommand { Name = "User2Label" };
        var labelResponse = await client2.PostAsJsonAsync("/api/labels", labelCommand);
        Assert.Equal(HttpStatusCode.Created, labelResponse.StatusCode);
        var labelResult = await labelResponse.Content.ReadFromJsonAsync<CreateLabelResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(labelResult);

        // User2 tries to assign their label to user1's task — should be 404 (task not found for user2)
        var response = await client2.PostAsync($"/api/tasks/{taskId}/labels/{labelResult.Id}", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        client2.Dispose();
    }

    [Fact]
    public async Task AssignLabel_OtherUsersLabel_Returns404()
    {
        var taskId = await CreateTestTask();

        // Register a second user and create a label
        var client2 = _factory.CreateClient();
        var register2 = new RegisterCommand
        {
            Email = "otheruser2@example.com",
            Password = "ValidPassword123",
            DisplayName = "Other User 2"
        };
        var registerResponse = await client2.PostAsJsonAsync("/api/auth/register", register2);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(registerResult);
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerResult.Token);

        var labelCommand = new CreateLabelCommand { Name = "OtherLabel" };
        var labelResponse = await client2.PostAsJsonAsync("/api/labels", labelCommand);
        Assert.Equal(HttpStatusCode.Created, labelResponse.StatusCode);
        var labelResult = await labelResponse.Content.ReadFromJsonAsync<CreateLabelResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(labelResult);

        // User1 tries to assign user2's label to their own task — should be 404 (label not found for user1)
        var response = await _client.PostAsync($"/api/tasks/{taskId}/labels/{labelResult.Id}", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        client2.Dispose();
    }
}
