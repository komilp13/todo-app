using TodoApp.Application.Services;
using Xunit;

namespace TodoApp.UnitTests.Services;

public class PasswordHashingServiceTests
{
    private readonly PasswordHashingService _service = new();

    [Fact]
    public void HashPassword_ReturnsHashAndSalt()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var (hash, salt) = _service.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotNull(salt);
        Assert.NotEmpty(salt);
    }

    [Fact]
    public void HashPassword_GeneratesUniqueSaltEachTime()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var (_, salt1) = _service.HashPassword(password);
        var (_, salt2) = _service.HashPassword(password);

        // Assert - Salts should be different
        Assert.NotEqual(salt1, salt2);
    }

    [Fact]
    public void HashPassword_ProducesDifferentHashesForSamePassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var (hash1, _) = _service.HashPassword(password);
        var (hash2, _) = _service.HashPassword(password);

        // Assert - Hashes should be different due to different salts
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForCorrectPassword()
    {
        // Arrange
        var password = "TestPassword123!";
        var (hash, salt) = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(password, hash, salt);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForIncorrectPassword()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var (hash, salt) = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(wrongPassword, hash, salt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForWrongSalt()
    {
        // Arrange
        var password = "TestPassword123!";
        var (hash, salt) = _service.HashPassword(password);
        var (_, wrongSalt) = _service.HashPassword("AnyPassword");

        // Act
        var result = _service.VerifyPassword(password, hash, wrongSalt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForEmptyPassword()
    {
        // Arrange
        var password = "TestPassword123!";
        var (hash, salt) = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword("", hash, salt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForInvalidBase64Salt()
    {
        // Arrange
        var password = "TestPassword123!";
        var invalidSalt = "not-valid-base64!!!";
        var hash = "somehash";

        // Act
        var result = _service.VerifyPassword(password, hash, invalidSalt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HashPassword_WithComplexPassword()
    {
        // Arrange
        var complexPassword = "P@ssw0rd!#$%^&*()_+-=[]{}|;:,.<>?/~`";

        // Act
        var (hash, salt) = _service.HashPassword(complexPassword);

        // Assert
        var verify = _service.VerifyPassword(complexPassword, hash, salt);
        Assert.True(verify);
    }

    [Fact]
    public void HashPassword_WithSimplePassword()
    {
        // Arrange
        var simplePassword = "abc123";

        // Act
        var (hash, salt) = _service.HashPassword(simplePassword);

        // Assert
        var verify = _service.VerifyPassword(simplePassword, hash, salt);
        Assert.True(verify);
    }

    [Fact]
    public void HashPassword_WithVeryLongPassword()
    {
        // Arrange
        var longPassword = new string('a', 500);

        // Act
        var (hash, salt) = _service.HashPassword(longPassword);

        // Assert
        var verify = _service.VerifyPassword(longPassword, hash, salt);
        Assert.True(verify);
    }

    [Fact]
    public void VerifyPassword_IsCaseSensitive()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongCasePassword = "testpassword123!";
        var (hash, salt) = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(wrongCasePassword, hash, salt);

        // Assert
        Assert.False(result);
    }
}
