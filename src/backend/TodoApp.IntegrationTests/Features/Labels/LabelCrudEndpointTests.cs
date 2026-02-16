using TodoApp.IntegrationTests.Base;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Features.Auth.Register;
using TodoApp.Application.Features.Labels.CreateLabel;
using TodoApp.Application.Features.Labels.GetLabels;
using TodoApp.Infrastructure.Persistence;
using Xunit;

namespace TodoApp.IntegrationTests.Features.Labels;

public class LabelCrudEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private string? _authToken;
    private Guid _userId;

    public LabelCrudEndpointTests()
    {
        var uniqueDbName = $"LabelCrudTests_{Guid.NewGuid()}";

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
            Email = "labeltest@example.com",
            Password = "ValidPassword123",
            DisplayName = "Label Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(registerResult);
        _authToken = registerResult.Token;

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "labeltest@example.com");
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

    private async Task<CreateLabelResponse> CreateTestLabel(string name = "Test Label", string? color = "#ff4040")
    {
        var request = new { name, color };
        var response = await _client.PostAsJsonAsync("/api/labels", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateLabelResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        return result;
    }

    // === CREATE ===

    [Fact]
    public async Task CreateLabel_WithValidData_Returns201()
    {
        var request = new { name = "Work", color = "#4073ff" };
        var response = await _client.PostAsJsonAsync("/api/labels", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateLabelResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal("Work", result.Name);
        Assert.Equal("#4073ff", result.Color);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateLabel_WithoutColor_Returns201()
    {
        var request = new { name = "NoColor" };
        var response = await _client.PostAsJsonAsync("/api/labels", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateLabelResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal("NoColor", result.Name);
        Assert.Null(result.Color);
    }

    [Fact]
    public async Task CreateLabel_EmptyName_Returns400()
    {
        var request = new { name = "", color = "#ff0000" };
        var response = await _client.PostAsJsonAsync("/api/labels", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLabel_NameTooLong_Returns400()
    {
        var request = new { name = new string('a', 101) };
        var response = await _client.PostAsJsonAsync("/api/labels", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLabel_InvalidColor_Returns400()
    {
        var request = new { name = "BadColor", color = "red" };
        var response = await _client.PostAsJsonAsync("/api/labels", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLabel_DuplicateName_Returns409()
    {
        await CreateTestLabel("Duplicate");

        var request = new { name = "Duplicate" };
        var response = await _client.PostAsJsonAsync("/api/labels", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateLabel_DuplicateNameCaseInsensitive_Returns409()
    {
        await CreateTestLabel("CaseTest");

        var request = new { name = "casetest" };
        var response = await _client.PostAsJsonAsync("/api/labels", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateLabel_Unauthenticated_Returns401()
    {
        var unauthClient = _factory.CreateClient();
        var request = new { name = "Unauth" };
        var response = await unauthClient.PostAsJsonAsync("/api/labels", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // === GET LIST ===

    [Fact]
    public async Task GetLabels_ReturnsAllUserLabels_SortedAlphabetically()
    {
        await CreateTestLabel("Zebra", "#000000");
        await CreateTestLabel("Alpha", "#ffffff");
        await CreateTestLabel("Middle", "#aaaaaa");

        var response = await _client.GetAsync("/api/labels");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetLabelsResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal(3, result.Labels.Count);
        Assert.Equal("Alpha", result.Labels[0].Name);
        Assert.Equal("Middle", result.Labels[1].Name);
        Assert.Equal("Zebra", result.Labels[2].Name);
    }

    [Fact]
    public async Task GetLabels_ReturnsTaskCount_ForOpenTasks()
    {
        var label = await CreateTestLabel("CountLabel", "#ff0000");

        // Create a task and assign the label
        var taskRequest = new { name = "Labeled Task", systemList = "inbox" };
        var taskResponse = await _client.PostAsJsonAsync("/api/tasks", taskRequest);
        Assert.Equal(HttpStatusCode.Created, taskResponse.StatusCode);
        var taskJson = await taskResponse.Content.ReadAsStringAsync();
        var taskDoc = System.Text.Json.JsonDocument.Parse(taskJson);
        var taskId = taskDoc.RootElement.GetProperty("id").GetString();

        // Assign label to task via direct DB manipulation
        var taskLabel = TodoApp.Domain.Entities.TaskLabel.Create(Guid.Parse(taskId!), label.Id);
        _dbContext.TaskLabels.Add(taskLabel);
        await _dbContext.SaveChangesAsync();

        var response = await _client.GetAsync("/api/labels");
        var result = await response.Content.ReadFromJsonAsync<GetLabelsResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);

        var countLabel = result.Labels.FirstOrDefault(l => l.Id == label.Id);
        Assert.NotNull(countLabel);
        Assert.Equal(1, countLabel.TaskCount);
    }

    [Fact]
    public async Task GetLabels_Empty_ReturnsEmptyList()
    {
        // Use a separate user with no labels
        var client = _factory.CreateClient();
        var registerRequest = new RegisterCommand
        {
            Email = "emptylabels@example.com",
            Password = "ValidPassword123",
            DisplayName = "Empty Labels User"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerResult!.Token);

        var response = await client.GetAsync("/api/labels");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetLabelsResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Empty(result.Labels);
        Assert.Equal(0, result.TotalCount);
    }

    // === UPDATE ===

    [Fact]
    public async Task UpdateLabel_Name_Returns200()
    {
        var label = await CreateTestLabel("OldName", "#ff0000");

        var request = new { name = "NewName" };
        var response = await _client.PutAsJsonAsync($"/api/labels/{label.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LabelItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal("NewName", result.Name);
        Assert.Equal("#ff0000", result.Color); // Color unchanged
    }

    [Fact]
    public async Task UpdateLabel_Color_Returns200()
    {
        var label = await CreateTestLabel("ColorUpdate", "#ff0000");

        var request = new { color = "#00ff00" };
        var response = await _client.PutAsJsonAsync($"/api/labels/{label.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LabelItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal("ColorUpdate", result.Name); // Name unchanged
        Assert.Equal("#00ff00", result.Color);
    }

    [Fact]
    public async Task UpdateLabel_ClearColor_Returns200()
    {
        var label = await CreateTestLabel("ClearColor", "#ff0000");

        var request = new Dictionary<string, object?> { { "color", null } };
        var response = await _client.PutAsJsonAsync($"/api/labels/{label.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LabelItemDto>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Null(result.Color);
    }

    [Fact]
    public async Task UpdateLabel_DuplicateName_Returns409()
    {
        await CreateTestLabel("Existing");
        var label = await CreateTestLabel("ToRename");

        var request = new { name = "Existing" };
        var response = await _client.PutAsJsonAsync($"/api/labels/{label.Id}", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateLabel_NotFound_Returns404()
    {
        var request = new { name = "Ghost" };
        var response = await _client.PutAsJsonAsync($"/api/labels/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // === DELETE ===

    [Fact]
    public async Task DeleteLabel_Returns204()
    {
        var label = await CreateTestLabel("ToDelete");

        var response = await _client.DeleteAsync($"/api/labels/{label.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's gone
        var getResponse = await _client.GetAsync("/api/labels");
        var result = await getResponse.Content.ReadFromJsonAsync<GetLabelsResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.DoesNotContain(result.Labels, l => l.Id == label.Id);
    }

    [Fact]
    public async Task DeleteLabel_RemovesTaskLabelAssociations()
    {
        var label = await CreateTestLabel("DeleteWithTasks", "#ff0000");

        // Create a task and assign the label
        var taskRequest = new { name = "Task With Label", systemList = "inbox" };
        var taskResponse = await _client.PostAsJsonAsync("/api/tasks", taskRequest);
        var taskJson = await taskResponse.Content.ReadAsStringAsync();
        var taskDoc = System.Text.Json.JsonDocument.Parse(taskJson);
        var taskId = Guid.Parse(taskDoc.RootElement.GetProperty("id").GetString()!);

        var taskLabel = TodoApp.Domain.Entities.TaskLabel.Create(taskId, label.Id);
        _dbContext.TaskLabels.Add(taskLabel);
        await _dbContext.SaveChangesAsync();

        // Delete label
        var response = await _client.DeleteAsync($"/api/labels/{label.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify TaskLabel is removed
        var remainingAssoc = await _dbContext.TaskLabels.AnyAsync(tl => tl.LabelId == label.Id);
        Assert.False(remainingAssoc);
    }

    [Fact]
    public async Task DeleteLabel_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/labels/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // === CROSS-USER ISOLATION ===

    [Fact]
    public async Task UpdateLabel_WrongUser_Returns404()
    {
        var label = await CreateTestLabel("User1Label");

        // Register a second user
        var client2 = _factory.CreateClient();
        var registerRequest = new RegisterCommand
        {
            Email = "labeluser2@example.com",
            Password = "ValidPassword123",
            DisplayName = "Label User 2"
        };
        var registerResponse = await client2.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerResult!.Token);

        var request = new { name = "Stolen" };
        var response = await client2.PutAsJsonAsync($"/api/labels/{label.Id}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteLabel_WrongUser_Returns404()
    {
        var label = await CreateTestLabel("User1LabelDel");

        var client2 = _factory.CreateClient();
        var registerRequest = new RegisterCommand
        {
            Email = "labeluser3@example.com",
            Password = "ValidPassword123",
            DisplayName = "Label User 3"
        };
        var registerResponse = await client2.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerResult!.Token);

        var response = await client2.DeleteAsync($"/api/labels/{label.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
