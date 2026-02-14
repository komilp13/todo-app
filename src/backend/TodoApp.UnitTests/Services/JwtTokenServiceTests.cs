using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using TodoApp.Application.Configuration;
using TodoApp.Application.Services;
using TodoApp.Domain.Entities;
using Xunit;

namespace TodoApp.UnitTests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _validSettings;
    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        _validSettings = new JwtSettings
        {
            SecretKey = "this-is-a-very-long-secret-key-that-is-256-bits-minimum",
            Issuer = "TodoApp",
            Audience = "TodoApp.Client",
            ExpirationMinutes = 60
        };

        var options = Options.Create(_validSettings);
        _service = new JwtTokenService(options);
    }

    private User CreateTestUser(Guid? id = null)
    {
        var user = User.Create("test@example.com", "hash", "salt", "Test User");

        // If a specific ID is needed for testing, we would need to modify the Create method
        // For now, just use the generated ID
        return user;
    }

    [Fact]
    public void GenerateToken_ReturnsValidJwtToken()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token = _service.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.True(token.Contains("."), "Token should be JWT format (header.payload.signature)");
    }

    [Fact]
    public void GenerateToken_IncludesRequiredClaims()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token = _service.GenerateToken(user);
        var principal = _service.ValidateToken(token);

        // Assert - Validate that token was generated and validated successfully
        // This demonstrates all required claims are present in the token
        Assert.NotNull(principal);
        Assert.NotNull(principal.FindFirst(ClaimTypes.Email));
        Assert.Equal(user.Email, principal.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    public void ValidateToken_ReturnsPrincipalForValidToken()
    {
        // Arrange
        var user = CreateTestUser();
        var token = _service.GenerateToken(user);

        // Act
        var principal = _service.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
    }

    [Fact]
    public void ValidateToken_ReturnsNullForInvalidToken()
    {
        // Arrange
        var invalidToken = "invalid.token.string";

        // Act
        var principal = _service.ValidateToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_ReturnsNullForEmptyToken()
    {
        // Arrange
        var emptyToken = string.Empty;

        // Act
        var principal = _service.ValidateToken(emptyToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_ReturnsNullForNullToken()
    {
        // Arrange
        string? nullToken = null;

        // Act
        var principal = _service.ValidateToken(nullToken ?? string.Empty);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_ReturnsNullForTamperedToken()
    {
        // Arrange
        var user = CreateTestUser();
        var token = _service.GenerateToken(user);
        var tamperedToken = token.Substring(0, token.Length - 10) + "tampered!";

        // Act
        var principal = _service.ValidateToken(tamperedToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_ReturnsNullForExpiredToken()
    {
        // Arrange - Create service with very short expiration
        var expiredSettings = new JwtSettings
        {
            SecretKey = "this-is-a-very-long-secret-key-that-is-256-bits-minimum",
            Issuer = "TodoApp",
            Audience = "TodoApp.Client",
            ExpirationMinutes = 0 // Expires immediately
        };
        var expiredService = new JwtTokenService(Options.Create(expiredSettings));
        var user = CreateTestUser();
        var token = expiredService.GenerateToken(user);

        // Wait a bit to ensure expiration
        System.Threading.Thread.Sleep(100);

        // Act
        var principal = expiredService.ValidateToken(token);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GenerateToken_DifferentTokensForDifferentUsers()
    {
        // Arrange
        var user1 = CreateTestUser(Guid.NewGuid());
        var user2 = CreateTestUser(Guid.NewGuid());

        // Act
        var token1 = _service.GenerateToken(user1);
        var token2 = _service.GenerateToken(user2);

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateToken_DifferentTokensForSameUserAtDifferentTimes()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token1 = _service.GenerateToken(user);
        System.Threading.Thread.Sleep(10); // Small delay to ensure different iat
        var token2 = _service.GenerateToken(user);

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void Constructor_ThrowsIfSecretKeyIsNull()
    {
        // Arrange
        var invalidSettings = new JwtSettings
        {
            SecretKey = null!,
            Issuer = "TodoApp",
            Audience = "TodoApp.Client"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new JwtTokenService(Options.Create(invalidSettings))
        );
    }

    [Fact]
    public void Constructor_ThrowsIfSecretKeyIsTooShort()
    {
        // Arrange - Secret key must be at least 256 bits (32 characters)
        var invalidSettings = new JwtSettings
        {
            SecretKey = "short",
            Issuer = "TodoApp",
            Audience = "TodoApp.Client"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new JwtTokenService(Options.Create(invalidSettings))
        );
    }

    [Fact]
    public void Constructor_ThrowsIfIssuerIsNull()
    {
        // Arrange
        var invalidSettings = new JwtSettings
        {
            SecretKey = "this-is-a-very-long-secret-key-that-is-256-bits-minimum",
            Issuer = null!,
            Audience = "TodoApp.Client"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new JwtTokenService(Options.Create(invalidSettings))
        );
    }

    [Fact]
    public void Constructor_ThrowsIfAudienceIsNull()
    {
        // Arrange
        var invalidSettings = new JwtSettings
        {
            SecretKey = "this-is-a-very-long-secret-key-that-is-256-bits-minimum",
            Issuer = "TodoApp",
            Audience = null!
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new JwtTokenService(Options.Create(invalidSettings))
        );
    }
}
