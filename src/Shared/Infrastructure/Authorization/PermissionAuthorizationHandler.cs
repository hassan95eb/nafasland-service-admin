using Microsoft.AspNetCore.Authorization;

namespace NafasLand.Admin.Shared.Infrastructure.Authorization;

/// <summary>
/// The access check is always on the permission, never on the role name (ADR-001).
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
