namespace NafasLand.Admin.Modules.Identity;

/// <summary>
/// Permission keys for the Identity module, as consts inside the module itself
/// (ADR-005), not in a central enum.
/// </summary>
internal static class IdentityPermissions
{
    /// <summary>Create user, change roles, activate/deactivate, reset another user's password.</summary>
    public const string UsersManage = "identity.users.manage";

    /// <summary>
    /// View/edit role permissions and grant/deny direct user permissions.
    /// IsSuperAdminOnly (ADR-002): this is access control itself — if Admin also
    /// had it, an Admin could grant themselves anything else.
    /// </summary>
    public const string AccessManage = "identity.access.manage";
}
