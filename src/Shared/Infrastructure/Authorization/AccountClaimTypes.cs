namespace NafasLand.Admin.Shared.Infrastructure.Authorization;

public static class AccountClaimTypes
{
    /// <summary>
    /// "true"/"false" claim recomputed on every request by
    /// EffectivePermissionsClaimsTransformation (ADR-023), read by
    /// AuthorizationBehavior to enforce the forced-password-change gate.
    /// </summary>
    public const string MustChangePassword = "must_change_password";
}
