namespace NafasLand.Admin.Modules.Identity.Security;

/// <summary>
/// Pure function for ADR-021's rule: effective permissions = (union of role
/// permissions) + (direct Grant) − (direct Deny), Deny always winning. Kept
/// separate from EffectivePermissionsProvider (which does the DB reads) so the
/// rule itself has a plain unit test with no database involved — the
/// prompt's required "effective permission calculation" test.
/// </summary>
internal static class EffectivePermissionCalculator
{
    public static IReadOnlySet<string> Calculate(
        IEnumerable<string> rolePermissionKeys,
        IEnumerable<string> grantedKeys,
        IEnumerable<string> deniedKeys)
    {
        var effective = new HashSet<string>(rolePermissionKeys, StringComparer.Ordinal);
        effective.UnionWith(grantedKeys);
        effective.ExceptWith(deniedKeys);
        return effective;
    }
}
