using System.Security.Claims;
using TodoApp.Domain.Entities;

namespace TodoApp.Domain.Interfaces;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT token for the given user.
    /// </summary>
    /// <param name="user">The user to generate a token for</param>
    /// <returns>A signed JWT token string</returns>
    string GenerateToken(User user);

    /// <summary>
    /// Validates a JWT token and extracts its claims.
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>The principal's claims if valid, null if invalid or expired</returns>
    ClaimsPrincipal? ValidateToken(string token);
}
