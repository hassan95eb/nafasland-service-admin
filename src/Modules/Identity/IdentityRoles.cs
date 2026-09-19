namespace NafasLand.Admin.Modules.Identity;

/// <summary>
/// Well-known role names (ADR-021, ADR-022). Access checks are always on
/// permission, never on these names (ADR-001) — they only identify the two
/// bootstrap-managed rows.
/// </summary>
internal static class IdentityRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
}
