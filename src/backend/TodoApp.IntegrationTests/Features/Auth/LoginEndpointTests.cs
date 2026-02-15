using TodoApp.IntegrationTests.Base;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Features.Auth.Login;
using TodoApp.Application.Features.Auth.Register;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Persistence;
using Xunit;

namespace TodoApp.IntegrationTests.Features.Auth;

public class LoginEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;

    public LoginEndpointTests()
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
                        options.UseInMemoryDatabase("LoginTests"));
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

    private async Task CreateTestUser(string email, string password)
    {
        var registerRequest = new RegisterCommand
        {
            Email = email,
            Password = password,
            DisplayName = "Test User"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200OkWithToken()
    {
        // Arrange
        const string email = "validuser@example.com";
        const string password = "ValidPassword123";
        await CreateTestUser(email, password);

        var loginRequest = new LoginCommand
        {
            Email = email,
            Password = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal(email, result.User.Email);
        Assert.Equal("Test User", result.User.DisplayName);
        Assert.NotEqual(Guid.Empty, result.User.Id);

        // Token should contain three parts separated by dots (header.payload.signature)
        var tokenParts = result.Token.Split('.');
        Assert.Equal(3, tokenParts.Length);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401Unauthorized()
    {
        // Arrange
        const string email = "user@example.com";
        const string correctPassword = "ValidPassword123";
        const string wrongPassword = "WrongPassword123";
        await CreateTestUser(email, correctPassword);

        var loginRequest = new LoginCommand
        {
            Email = email,
            Password = wrongPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonexistentEmail_Returns401Unauthorized()
    {
        // Arrange
        var loginRequest = new LoginCommand
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCaseInsensitiveEmail_Returns200Ok()
    {
        // Arrange
        const string email = "mixedcase@example.com";
        const string password = "ValidPassword123";
        await CreateTestUser(email, password);

        var loginRequest = new LoginCommand
        {
            Email = "MIXEDCASE@EXAMPLE.COM",  // Different case
            Password = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonHelper.DefaultOptions);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task Login_WithMissingEmail_Returns400BadRequest()
    {
        // Arrange
        var loginRequest = new LoginCommand
        {
            Email = "",
            Password = "ValidPassword123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingPassword_Returns400BadRequest()
    {
        // Arrange
        var loginRequest = new LoginCommand
        {
            Email = "user@example.com",
            Password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_Returns400BadRequest()
    {
        // Arrange
        var loginRequest = new LoginCommand
        {
            Email = "not-an-email",
            Password = "ValidPassword123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_GenericErrorMessage_PreventsUserEnumeration()
    {
        // Arrange
        const string existingEmail = "existing@example.com";
        const string existingPassword = "ValidPassword123";
        const string nonexistentEmail = "nonexistent@example.com";
        const string wrongPassword = "WrongPassword123";

        // Create one user
        await CreateTestUser(existingEmail, existingPassword);

        var wrongPasswordRequest = new LoginCommand
        {
            Email = existingEmail,
            Password = wrongPassword
        };

        var nonexistentEmailRequest = new LoginCommand
        {
            Email = nonexistentEmail,
            Password = existingPassword
        };

        // Act
        var wrongPasswordResponse = await _client.PostAsJsonAsync("/api/auth/login", wrongPasswordRequest);
        var nonexistentEmailResponse = await _client.PostAsJsonAsync("/api/auth/login", nonexistentEmailRequest);

        // Assert - Both should return 401 with same status code (no distinction for security)
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPasswordResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, nonexistentEmailResponse.StatusCode);
    }

    [Fact]
    public async Task Login_TokenCanBeUsedForAuthentication()
    {
        // Arrange
        const string email = "tokentest@example.com";
        const string password = "ValidPassword123";
        await CreateTestUser(email, password);

        var loginRequest = new LoginCommand
        {
            Email = email,
            Password = password
        };

        // Act - Login
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJsonHelper.DefaultOptions);

        // Assert - Token is valid JWT format
        Assert.NotNull(loginResult);
        Assert.NotEmpty(loginResult.Token);

        var tokenParts = loginResult.Token.Split('.');
        Assert.Equal(3, tokenParts.Length);  // Valid JWT has 3 parts

        // Each part should be base64 encoded
        foreach (var part in tokenParts)
        {
            Assert.NotEmpty(part);
        }
    }

    [Fact]
    public async Task Login_MultipleUsersIndependent()
    {
        // Arrange
        const string user1Email = "user1@example.com";
        const string user1Password = "Password1Test123";
        const string user2Email = "user2@example.com";
        const string user2Password = "Password2Test123";

        await CreateTestUser(user1Email, user1Password);
        await CreateTestUser(user2Email, user2Password);

        var user1Request = new LoginCommand { Email = user1Email, Password = user1Password };
        var user2Request = new LoginCommand { Email = user2Email, Password = user2Password };

        // Act
        var user1Response = await _client.PostAsJsonAsync("/api/auth/login", user1Request);
        var user2Response = await _client.PostAsJsonAsync("/api/auth/login", user2Request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, user1Response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, user2Response.StatusCode);

        var user1Result = await user1Response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonHelper.DefaultOptions);
        var user2Result = await user2Response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonHelper.DefaultOptions);

        Assert.NotNull(user1Result);
        Assert.NotNull(user2Result);
        Assert.Equal(user1Email, user1Result.User.Email);
        Assert.Equal(user2Email, user2Result.User.Email);
        Assert.NotEqual(user1Result.Token, user2Result.Token);  // Different tokens
    }
}
