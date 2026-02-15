using TodoApp.IntegrationTests.Base;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Features.Auth.CurrentUser;
using TodoApp.Application.Features.Auth.Login;
using TodoApp.Application.Features.Auth.Register;
using TodoApp.Infrastructure.Persistence;
using Xunit;

namespace TodoApp.IntegrationTests.Features.Auth;

public class CurrentUserEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;

    public CurrentUserEndpointTests()
    {
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
                        options.UseInMemoryDatabase("CurrentUserTests"));
                });
            });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        _scope.Dispose();
        _factory.Dispose();
    }

    private async Task<string> RegisterAndLogin(string email, string password)
    {
        // Register user
        var registerRequest = new RegisterCommand
        {
            Email = email,
            Password = password,
            DisplayName = "Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);

        if (registerResult == null || string.IsNullOrEmpty(registerResult.Token))
        {
            // Try logging in if already registered
            var loginRequest = new LoginCommand { Email = email, Password = password };
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJsonHelper.DefaultOptions);
            return loginResult?.Token ?? string.Empty;
        }

        return registerResult.Token;
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_Returns200OkWithUserData()
    {
        // Arrange
        const string email = "validuser@example.com";
        const string password = "ValidPassword123";
        const string displayName = "Valid User";

        var registerRequest = new RegisterCommand
        {
            Email = email,
            Password = password,
            DisplayName = displayName
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        var token = registerResult?.Token;

        // Act - Call /me endpoint with valid token
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.Equal(displayName, result.DisplayName);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.True(result.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_Returns401Unauthorized()
    {
        // Act - Call /me endpoint without token
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidToken_Returns401Unauthorized()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.token.here");
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithMalformedToken_Returns401Unauthorized()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-jwt-token");
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_AfterUserDeleted_Returns404NotFound()
    {
        // Arrange
        const string email = "deleteduser@example.com";
        const string password = "ValidPassword123";

        // Register and login
        var token = await RegisterAndLogin(email, password);
        Assert.NotEmpty(token);

        // Get current user to get the ID
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        var userData = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonHelper.DefaultOptions);
        var userId = userData?.Id ?? Guid.Empty;

        // Manually delete the user from the database
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null)
        {
            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
        }

        // Act - Try to get current user with valid token but non-existent user
        var request2 = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response2 = await _client.SendAsync(request2);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_MultipleUsers_ReturnCorrectData()
    {
        // Arrange
        const string user1Email = "user1@example.com";
        const string user1Password = "Password1Test123";
        const string user1DisplayName = "User One";

        const string user2Email = "user2@example.com";
        const string user2Password = "Password2Test123";
        const string user2DisplayName = "User Two";

        // Register both users
        var register1 = new RegisterCommand
        {
            Email = user1Email,
            Password = user1Password,
            DisplayName = user1DisplayName
        };
        var response1 = await _client.PostAsJsonAsync("/api/auth/register", register1);
        var result1 = await response1.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        var token1 = result1?.Token ?? string.Empty;

        var register2 = new RegisterCommand
        {
            Email = user2Email,
            Password = user2Password,
            DisplayName = user2DisplayName
        };
        var response2 = await _client.PostAsJsonAsync("/api/auth/register", register2);
        var result2 = await response2.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        var token2 = result2?.Token ?? string.Empty;

        // Act - Get current user for both
        var request1 = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request1.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);
        var getCurrentUser1 = await _client.SendAsync(request1);
        var currentUser1 = await getCurrentUser1.Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonHelper.DefaultOptions);

        var request2 = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token2);
        var getCurrentUser2 = await _client.SendAsync(request2);
        var currentUser2 = await getCurrentUser2.Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonHelper.DefaultOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, getCurrentUser1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getCurrentUser2.StatusCode);

        Assert.NotNull(currentUser1);
        Assert.NotNull(currentUser2);

        Assert.Equal(user1Email, currentUser1.Email);
        Assert.Equal(user1DisplayName, currentUser1.DisplayName);

        Assert.Equal(user2Email, currentUser2.Email);
        Assert.Equal(user2DisplayName, currentUser2.DisplayName);

        // Different users should have different IDs
        Assert.NotEqual(currentUser1.Id, currentUser2.Id);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsCorrectCreatedAtTimestamp()
    {
        // Arrange
        const string email = "timestamp@example.com";
        const string password = "ValidPassword123";

        var beforeRegister = DateTime.UtcNow;

        var registerRequest = new RegisterCommand
        {
            Email = email,
            Password = password,
            DisplayName = "Timestamp User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(TestJsonHelper.DefaultOptions);
        var token = registerResult?.Token ?? string.Empty;

        var afterRegister = DateTime.UtcNow;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        var currentUser = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonHelper.DefaultOptions);

        // Assert
        Assert.NotNull(currentUser);
        // CreatedAt should be between registration request and response
        Assert.True(currentUser.CreatedAt >= beforeRegister);
        Assert.True(currentUser.CreatedAt <= afterRegister);
    }
}
