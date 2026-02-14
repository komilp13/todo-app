namespace TodoApp.Domain.Interfaces;

/// <summary>
/// Service for securely hashing and verifying passwords using PBKDF2 with HMAC-SHA256.
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Hashes a password with a cryptographically random salt using PBKDF2-SHA256 (100k+ iterations).
    /// </summary>
    /// <param name="password">The plaintext password to hash</param>
    /// <returns>A tuple containing (hash, salt) both as Base64-encoded strings</returns>
    (string hash, string salt) HashPassword(string password);

    /// <summary>
    /// Verifies a plaintext password against a stored hash and salt.
    /// </summary>
    /// <param name="password">The plaintext password to verify</param>
    /// <param name="hash">The stored Base64-encoded hash</param>
    /// <param name="salt">The stored Base64-encoded salt</param>
    /// <returns>True if the password matches the hash, false otherwise</returns>
    bool VerifyPassword(string password, string hash, string salt);
}
