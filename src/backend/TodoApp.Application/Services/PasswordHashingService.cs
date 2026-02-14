using System.Security.Cryptography;
using TodoApp.Domain.Interfaces;

namespace TodoApp.Application.Services;

/// <summary>
/// Implements password hashing using PBKDF2 with HMAC-SHA256.
/// Each password is hashed with a unique random salt (32 bytes).
/// Uses 100,000 iterations for strong security against brute-force attacks.
/// </summary>
public class PasswordHashingService : IPasswordHashingService
{
    // PBKDF2 configuration
    private const int SaltSize = 32; // 256 bits
    private const int IterationCount = 100000;
    private const int KeySize = 32; // 256 bits
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <summary>
    /// Hashes a password with a cryptographically random salt using PBKDF2-SHA256.
    /// </summary>
    public (string hash, string salt) HashPassword(string password)
    {
        // Generate a random salt
        using (var rng = RandomNumberGenerator.Create())
        {
            var saltBytes = new byte[SaltSize];
            rng.GetBytes(saltBytes);

            // Hash the password with the salt
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                saltBytes,
                IterationCount,
                Algorithm))
            {
                var hashBytes = pbkdf2.GetBytes(KeySize);

                // Return both hash and salt as Base64 strings
                return (
                    Convert.ToBase64String(hashBytes),
                    Convert.ToBase64String(saltBytes)
                );
            }
        }
    }

    /// <summary>
    /// Verifies a plaintext password against a stored hash and salt.
    /// </summary>
    public bool VerifyPassword(string password, string hash, string salt)
    {
        try
        {
            // Decode salt from Base64
            var saltBytes = Convert.FromBase64String(salt);

            // Hash the provided password with the stored salt
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                saltBytes,
                IterationCount,
                Algorithm))
            {
                var hashBytes = pbkdf2.GetBytes(KeySize);
                var computedHash = Convert.ToBase64String(hashBytes);

                // Compare the computed hash with the stored hash
                // Use constant-time comparison to prevent timing attacks
                return ConstantTimeComparison(computedHash, hash);
            }
        }
        catch
        {
            // If anything goes wrong (bad Base64, etc.), return false
            return false;
        }
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks.
    /// </summary>
    private static bool ConstantTimeComparison(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}
