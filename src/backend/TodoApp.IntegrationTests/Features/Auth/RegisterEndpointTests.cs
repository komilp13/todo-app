using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Application.Features.Auth.Register;
using TodoApp.Infrastructure.Persistence;
using Xunit;

namespace TodoApp.IntegrationTests.Features.Auth;

public class RegisterEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;

    public RegisterEndpointTests()
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
                        options.UseInMemoryDatabase("RegisterTests"));
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

    [Fact]
    public async Task Register_WithValidData_Returns201Created()
    {
        // Arrange
        var request = new RegisterCommand
        {
            Email = "newuser@example.com",
            Password = "ValidPassword123",
            DisplayName = "New User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal("newuser@example.com", result.User.Email);
        Assert.Equal("New User", result.User.DisplayName);
        Assert.NotEqual(Guid.Empty, result.User.Id);

        // Verify user was created in database
        var createdUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "newuser@example.com");
        Assert.NotNull(createdUser);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409Conflict()
    {
        // Arrange
        var request1 = new RegisterCommand
        {
            Email = "duplicate@example.com",
            Password = "ValidPassword123",
            DisplayName = "First User"
        };

        var request2 = new RegisterCommand
        {
            Email = "duplicate@example.com",
            Password = "AnotherPassword123",
            DisplayName = "Second User"
        };

        // Act - Register first user
        await _client.PostAsJsonAsync("/api/auth/register", request1);

        // Act - Try to register with same email
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterCommand
        {
            Email = "not-an-email",
            Password = "ValidPassword123",
            DisplayName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterCommand
        {
            Email = "user@example.com",
            Password = "Short1",  // Less than 8 characters
            DisplayName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithPasswordMissingUppercase_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterCommand
        {
            Email = "user@example.com",
            Password = "validpassword123",  // No uppercase
            DisplayName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithPasswordMissingLowercase_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterCommand
        {
            Email = "user@example.com",
            Password = "VALIDPASSWORD123",  // No lowercase
            DisplayName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithPasswordMissingDigit_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterCommand
        {
            Email = "user@example.com",
            Password = "ValidPassword",  // No digit
            DisplayName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterCommand
        {
            Email = "",
            Password = "ValidPassword123",
            DisplayName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingPassword_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterCommand
        {
            Email = "user@example.com",
            Password = "",
            DisplayName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingDisplayName_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterCommand
        {
            Email = "user@example.com",
            Password = "ValidPassword123",
            DisplayName = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithValidToken_CanBeValidated()
    {
        // Arrange
        var request = new RegisterCommand
        {
            Email = "tokentest@example.com",
            Password = "ValidPassword123",
            DisplayName = "Token Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        // Token should contain three parts separated by dots (header.payload.signature)
        var tokenParts = result.Token.Split('.');
        Assert.Equal(3, tokenParts.Length);
    }
}
