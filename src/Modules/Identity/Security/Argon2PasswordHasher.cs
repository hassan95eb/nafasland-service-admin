using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace NafasLand.Admin.Modules.Identity.Security;

/// <summary>
/// Argon2id password hashing (ADR-023), via Konscious.Security.Cryptography.Argon2
/// (pre-approved for step 1). The encoded hash embeds its own cost parameters
/// (<c>argon2id$m=..$t=..$p=..$&lt;salt&gt;$&lt;hash&gt;</c>) so tuning them later
/// does not invalidate passwords hashed under the old parameters.
/// </summary>
internal sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int MemorySizeKb = 65536; // 64 MB
    private const int Iterations = 3;
    private const int Parallelism = 2;

    public string Algorithm => "Argon2id";

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = ComputeHash(password, salt, MemorySizeKb, Iterations, Parallelism, HashSizeBytes);
        return string.Join(
            '$',
            "argon2id",
            $"m={MemorySizeKb}",
            $"t={Iterations}",
            $"p={Parallelism}",
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string encodedHash)
    {
        var parts = encodedHash.Split('$');
        if (parts.Length != 6 || parts[0] != "argon2id")
        {
            return false;
        }

        try
        {
            var memorySizeKb = int.Parse(parts[1]["m=".Length..]);
            var iterations = int.Parse(parts[2]["t=".Length..]);
            var parallelism = int.Parse(parts[3]["p=".Length..]);
            var salt = Convert.FromBase64String(parts[4]);
            var expectedHash = Convert.FromBase64String(parts[5]);

            var actualHash = ComputeHash(password, salt, memorySizeKb, iterations, parallelism, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] ComputeHash(
        string password,
        byte[] salt,
        int memorySizeKb,
        int iterations,
        int parallelism,
        int hashSizeBytes)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = parallelism,
            Iterations = iterations,
            MemorySize = memorySizeKb,
        };

        return argon2.GetBytes(hashSizeBytes);
    }
}
