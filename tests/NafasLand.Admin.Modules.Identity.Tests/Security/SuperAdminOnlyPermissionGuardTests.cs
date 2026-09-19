using NafasLand.Admin.Modules.Identity.Security;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Identity.Tests.Security;

/// <summary>The required "reject Grant/Deny on a SuperAdminOnly permission for a non-SuperAdmin target" test, at the guard level shared by SetUserPermission and SetRolePermissions.</summary>
public sealed class SuperAdminOnlyPermissionGuardTests
{
    [Fact]
    public void اعطای_permission_SuperAdminOnly_به_هدف_غیر_SuperAdmin_رد_می‌شود()
    {
        Assert.Throws<CommandValidationException>(() =>
            SuperAdminOnlyPermissionGuard.EnsureGrantAllowed("identity.access.manage", isSuperAdminOnly: true, targetIsSuperAdmin: false));
    }

    [Fact]
    public void اعطای_permission_SuperAdminOnly_به_هدف_SuperAdmin_مجاز_است()
    {
        var exception = Record.Exception(() =>
            SuperAdminOnlyPermissionGuard.EnsureGrantAllowed("identity.access.manage", isSuperAdminOnly: true, targetIsSuperAdmin: true));

        Assert.Null(exception);
    }

    [Fact]
    public void اعطای_permission_معمولی_به_هر_هدفی_مجاز_است()
    {
        var exception = Record.Exception(() =>
            SuperAdminOnlyPermissionGuard.EnsureGrantAllowed("identity.users.manage", isSuperAdminOnly: false, targetIsSuperAdmin: false));

        Assert.Null(exception);
    }
}
