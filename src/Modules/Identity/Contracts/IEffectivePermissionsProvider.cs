namespace NafasLand.Admin.Modules.Identity.Contracts;

/// <summary>
/// Computes a user's effective access (permissions plus the forced-password-change
/// flag) fresh from the database. Consumed by EffectivePermissionsClaimsTransformation
/// (in Api) on every request, so a Grant/Deny or role change by SuperAdmin takes
/// effect without the affected user needing to log in again (ADR-021).
/// </summary>
public interface IEffectivePermissionsProvider
{
    /// <summary>
    /// Returns null when the user id no longer resolves to an account (fails
    /// closed: no permissions, not "keep whatever was there before").
    /// </summary>
    Task<EffectiveUserAccess?> GetEffectiveAccessAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// Effective permissions = (union of role permissions) + (direct Grant) −
/// (direct Deny); Deny always wins (ADR-021). MustChangePassword is carried here
/// too so it can be recomputed on the same per-request cadence as permissions.
/// </summary>
public sealed record EffectiveUserAccess(IReadOnlyCollection<string> Permissions, bool MustChangePassword);
