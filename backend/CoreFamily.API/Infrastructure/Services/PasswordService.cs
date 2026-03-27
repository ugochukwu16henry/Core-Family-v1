using System.Security.Cryptography;
using System.Text;
using CoreFamily.API.Application.Interfaces;

namespace CoreFamily.API.Infrastructure.Services;

/// <summary>
/// Uses PBKDF2-SHA512 with 350,000 iterations — OWASP recommended minimum for 2026.
/// </summary>
public class PasswordService : IPasswordService
{
    private const int SaltSize = 32;
    private const int HashSize = 64;
    private const int Iterations = 350_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            HashSize);

        // Format: iterations.salt.hash (all base64)
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3) return false;

        if (!int.TryParse(parts[0], out var iterations)) return false;
        var salt = Convert.FromBase64String(parts[1]);
        var hash = Convert.FromBase64String(parts[2]);

        var inputHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            Algorithm,
            hash.Length);

        return CryptographicOperations.FixedTimeEquals(hash, inputHash);
    }
}
