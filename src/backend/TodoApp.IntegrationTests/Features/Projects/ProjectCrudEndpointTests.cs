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
/// Integration tests for Get, Update, Complete, Reopen, and Delete project endpoints (Story 6.1.2).
/// </summary>
public class ProjectCrudEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private string? _authToken;
    private Guid _userId;

    public ProjectCrudEndpointTests()
    {
        var uniqueDbName = $"ProjectCrudTests_{Guid.NewGuid()}";

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
            Email = "projectcrud@example.com",
            Password = "ValidPassword123",
            DisplayName = "Project CRUD Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(registerResult);
        _authToken = registerResult.Token;

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "projectcrud@example.com");
        Assert.NotNull(user);
        _userId = user.Id;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        _scope.Dispose();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<CreateProjectResponse> CreateTestProject(string name = "Test Project", string? description = null)
    {
        var request = new CreateProjectCommand { Name = name, Description = description };
        var response = await _client.PostAsJsonAsync("/api/projects", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateProjectResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        return result;
    }

    // ==================== GET /api/projects/{id} ====================

    [Fact]
    public async Task GetProjectById_WithValidId_Returns200WithStats()
    {
        var created = await CreateTestProject("My Project", "A description");

        // Add tasks to the project
        await _client.PostAsJsonAsync("/api/tasks", new CreateTaskCommand { Name = "Task 1", ProjectId = created.Id });
        await _client.PostAsJsonAsync("/api/tasks", new CreateTaskCommand { Name = "Task 2", ProjectId = created.Id });
        var task3Resp = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskCommand { Name = "Task 3", ProjectId = created.Id });
        var task3 = await task3Resp.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);
        await _client.PatchAsync($"/api/tasks/{task3!.Id}/complete", null);

        var response = await _client.GetAsync($"/api/projects/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(project);
        Assert.Equal(created.Id, project.Id);
        Assert.Equal("My Project", project.Name);
        Assert.Equal("A description", project.Description);
        Assert.Equal(ProjectStatus.Active, project.Status);
        Assert.Equal(3, project.TotalTaskCount);
        Assert.Equal(1, project.CompletedTaskCount);
        Assert.Equal(33, project.CompletionPercentage);
    }

    [Fact]
    public async Task GetProjectById_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/projects/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectById_InvalidGuid_Returns400()
    {
        var response = await _client.GetAsync("/api/projects/not-a-guid");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectById_OtherUsersProject_Returns404()
    {
        var created = await CreateTestProject("User1 Project");

        // Create second user
        var register2 = new RegisterCommand
        {
            Email = "projectcrud2@example.com",
            Password = "ValidPassword123",
            DisplayName = "User 2"
        };
        var regResp = await _client.PostAsJsonAsync("/api/auth/register", register2);
        var regResult = await regResp.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", regResult!.Token);

        var response = await _client.GetAsync($"/api/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ==================== PUT /api/projects/{id} ====================

    [Fact]
    public async Task UpdateProject_Name_Returns200WithUpdatedProject()
    {
        var created = await CreateTestProject("Original Name");

        var response = await _client.PutAsJsonAsync($"/api/projects/{created.Id}",
            new Dictionary<string, object?> { ["name"] = "Updated Name" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(project);
        Assert.Equal("Updated Name", project.Name);
        Assert.True(project.UpdatedAt > created.UpdatedAt);
    }

    [Fact]
    public async Task UpdateProject_Description_Returns200()
    {
        var created = await CreateTestProject("Project");

        var response = await _client.PutAsJsonAsync($"/api/projects/{created.Id}",
            new Dictionary<string, object?> { ["description"] = "New description" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(project);
        Assert.Equal("New description", project.Description);
    }

    [Fact]
    public async Task UpdateProject_ClearDescription_Returns200()
    {
        var created = await CreateTestProject("Project", "Original description");

        var response = await _client.PutAsJsonAsync($"/api/projects/{created.Id}",
            new Dictionary<string, object?> { ["description"] = null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(project);
        Assert.Null(project.Description);
    }

    [Fact]
    public async Task UpdateProject_DueDate_Returns200()
    {
        var created = await CreateTestProject("Project");
        var dueDate = DateTime.UtcNow.AddDays(30).ToString("o");

        var response = await _client.PutAsJsonAsync($"/api/projects/{created.Id}",
            new Dictionary<string, object?> { ["dueDate"] = dueDate });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(project);
        Assert.NotNull(project.DueDate);
    }

    [Fact]
    public async Task UpdateProject_NotFound_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/projects/{Guid.NewGuid()}",
            new Dictionary<string, object?> { ["name"] = "Updated" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_NameTooLong_Returns400()
    {
        var created = await CreateTestProject("Project");

        var response = await _client.PutAsJsonAsync($"/api/projects/{created.Id}",
            new Dictionary<string, object?> { ["name"] = new string('a', 201) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ==================== PATCH /api/projects/{id}/complete ====================

    [Fact]
    public async Task CompleteProject_Returns200WithCompletedStatus()
    {
        var created = await CreateTestProject("Project to Complete");

        var response = await _client.PatchAsync($"/api/projects/{created.Id}/complete", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(project);
        Assert.Equal(ProjectStatus.Completed, project.Status);
    }

    [Fact]
    public async Task CompleteProject_DoesNotCompleteTasks()
    {
        var created = await CreateTestProject("Project");
        await _client.PostAsJsonAsync("/api/tasks", new CreateTaskCommand { Name = "Open Task", ProjectId = created.Id });

        await _client.PatchAsync($"/api/projects/{created.Id}/complete", null);

        // Verify tasks are still open
        var tasksResp = await _client.GetAsync($"/api/tasks?projectId={created.Id}");
        var tasksJson = await tasksResp.Content.ReadAsStringAsync();
        Assert.Contains("Open Task", tasksJson);
    }

    [Fact]
    public async Task CompleteProject_Idempotent_Returns200()
    {
        var created = await CreateTestProject("Project");

        await _client.PatchAsync($"/api/projects/{created.Id}/complete", null);
        var response = await _client.PatchAsync($"/api/projects/{created.Id}/complete", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(project);
        Assert.Equal(ProjectStatus.Completed, project.Status);
    }

    [Fact]
    public async Task CompleteProject_NotFound_Returns404()
    {
        var response = await _client.PatchAsync($"/api/projects/{Guid.NewGuid()}/complete", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ==================== PATCH /api/projects/{id}/reopen ====================

    [Fact]
    public async Task ReopenProject_Returns200WithActiveStatus()
    {
        var created = await CreateTestProject("Project");
        await _client.PatchAsync($"/api/projects/{created.Id}/complete", null);

        var response = await _client.PatchAsync($"/api/projects/{created.Id}/reopen", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(project);
        Assert.Equal(ProjectStatus.Active, project.Status);
    }

    [Fact]
    public async Task ReopenProject_AlreadyActive_Idempotent()
    {
        var created = await CreateTestProject("Project");

        var response = await _client.PatchAsync($"/api/projects/{created.Id}/reopen", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(project);
        Assert.Equal(ProjectStatus.Active, project.Status);
    }

    [Fact]
    public async Task ReopenProject_NotFound_Returns404()
    {
        var response = await _client.PatchAsync($"/api/projects/{Guid.NewGuid()}/reopen", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ==================== DELETE /api/projects/{id} ====================

    [Fact]
    public async Task DeleteProject_Returns204()
    {
        var created = await CreateTestProject("Project to Delete");

        var response = await _client.DeleteAsync($"/api/projects/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify project is gone
        var getResp = await _client.GetAsync($"/api/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_OrphansTasks()
    {
        var created = await CreateTestProject("Project");
        var taskResp = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskCommand { Name = "Orphaned Task", ProjectId = created.Id });
        var task = await taskResp.Content.ReadFromJsonAsync<CreateTaskResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(task);

        await _client.DeleteAsync($"/api/projects/{created.Id}");

        // Verify task still exists but has no project
        var getTaskResp = await _client.GetAsync($"/api/tasks/{task.Id}");
        Assert.Equal(HttpStatusCode.OK, getTaskResp.StatusCode);
        var taskJson = await getTaskResp.Content.ReadAsStringAsync();
        Assert.Contains("Orphaned Task", taskJson);
        // projectId should be null
        Assert.Contains("\"projectId\":null", taskJson);
    }

    [Fact]
    public async Task DeleteProject_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/projects/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_OtherUsersProject_Returns404()
    {
        var created = await CreateTestProject("User1 Project");

        // Create second user
        var register2 = new RegisterCommand
        {
            Email = "projectcrud3@example.com",
            Password = "ValidPassword123",
            DisplayName = "User 3"
        };
        var regResp = await _client.PostAsJsonAsync("/api/auth/register", register2);
        var regResult = await regResp.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", regResult!.Token);

        var response = await _client.DeleteAsync($"/api/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ==================== Auth tests ====================

    [Fact]
    public async Task AllEndpoints_WithoutAuth_Return401()
    {
        var noAuthClient = _factory.CreateClient();
        var id = Guid.NewGuid();

        Assert.Equal(HttpStatusCode.Unauthorized, (await noAuthClient.GetAsync($"/api/projects/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await noAuthClient.PutAsJsonAsync($"/api/projects/{id}", new Dictionary<string, object?> { ["name"] = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await noAuthClient.PatchAsync($"/api/projects/{id}/complete", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await noAuthClient.PatchAsync($"/api/projects/{id}/reopen", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await noAuthClient.DeleteAsync($"/api/projects/{id}")).StatusCode);

        noAuthClient.Dispose();
    }
}
