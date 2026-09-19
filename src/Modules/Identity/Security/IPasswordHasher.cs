namespace NafasLand.Admin.Modules.Identity.Security;

internal interface IPasswordHasher
{
    /// <summary>Value stored in AppUser.PasswordAlgorithm for newly hashed passwords.</summary>
    string Algorithm { get; }

    string Hash(string password);

    bool Verify(string password, string encodedHash);
}
