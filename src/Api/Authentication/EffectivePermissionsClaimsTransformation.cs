using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using NafasLand.Admin.Modules.Identity.Contracts;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Api.Authentication;

/// <summary>
/// Recomputes the signed-in user's permission claims and MustChangePassword flag
/// on every request (ADR-021), by asking Identity's IEffectivePermissionsProvider
/// — the only Identity type this host touches directly, and only through
/// Contracts, per the step-01 prompt. The session cookie itself only ever carries
/// NameIdentifier/Name (set once, at login); everything else is added here, fresh,
/// each time.
/// </summary>
internal sealed class EffectivePermissionsClaimsTransformation(IEffectivePermissionsProvider effectivePermissionsProvider)
    : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated is not true)
        {
            return principal;
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return principal;
        }

        var access = await effectivePermissionsProvider.GetEffectiveAccessAsync(userId, CancellationToken.None);
        if (access is null)
        {
            // Account no longer resolves (e.g. deleted) — fail closed, no permissions.
            return principal;
        }

        var identity = (ClaimsIdentity)principal.Identity;
        foreach (var permission in access.Permissions)
        {
            identity.AddClaim(new Claim(PermissionClaimTypes.Permission, permission));
        }

        identity.AddClaim(new Claim(AccountClaimTypes.MustChangePassword, access.MustChangePassword ? "true" : "false"));

        return principal;
    }
}
