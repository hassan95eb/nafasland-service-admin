using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Identity.Security;

/// <summary>
/// ADR-046: assigning the SuperAdmin role to any account is itself a
/// SuperAdmin-only action, even for a caller who otherwise holds
/// <c>identity.users.manage</c>. Without this, an Admin holding only
/// <c>identity.users.manage</c> could grant themselves (or anyone) the
/// SuperAdmin role through SetUserRoles — the same self-escalation ADR-002's
/// IsSuperAdminOnly flag exists to prevent for permissions, just reached
/// through a role assignment instead of a direct permission grant.
/// </summary>
internal static class SuperAdminRoleAssignmentGuard
{
    public static void EnsureAssignable(bool targetRoleSetIncludesSuperAdmin, bool actorHasAccessManage)
    {
        if (targetRoleSetIncludesSuperAdmin && !actorHasAccessManage)
        {
            throw new CommandValidationException(new Dictionary<string, string[]>
            {
                ["roleIds"] = ["انتساب نقش SuperAdmin فقط با دسترسی identity.access.manage مجاز است."],
            });
        }
    }
}
