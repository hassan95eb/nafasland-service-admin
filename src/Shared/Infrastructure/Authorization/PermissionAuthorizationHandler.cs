using Microsoft.AspNetCore.Authorization;

namespace NafasLand.Admin.Shared.Infrastructure.Authorization;

/// <summary>
/// چک دسترسی همیشه روی permission است، هرگز روی نام نقش (ADR-001).
/// </summary>
internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim(PermissionClaimTypes.Permission, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
