using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Identity.Security;

/// <summary>
/// ADR-002's IsSuperAdminOnly flag: such a permission can never be Granted to a
/// role or user that is not itself SuperAdmin. Shared by SetUserPermission (per
/// user) and SetRolePermissions (per role) so the rule is written, and tested,
/// exactly once.
/// </summary>
internal static class SuperAdminOnlyPermissionGuard
{
    public static void EnsureGrantAllowed(string permissionKey, bool isSuperAdminOnly, bool targetIsSuperAdmin)
    {
        if (isSuperAdminOnly && !targetIsSuperAdmin)
        {
            throw new CommandValidationException(new Dictionary<string, string[]>
            {
                ["permissionKeys"] =
                    [$"permission «{permissionKey}» فقط مخصوص SuperAdmin است و به هدف غیر-SuperAdmin قابل اعطا نیست."],
            });
        }
    }
}
