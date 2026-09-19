using NafasLand.Admin.Modules.Identity.Security;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Identity.Tests.Security;

/// <summary>ADR-046: assigning the SuperAdmin role needs identity.access.manage, not just identity.users.manage.</summary>
public sealed class SuperAdminRoleAssignmentGuardTests
{
    [Fact]
    public void انتساب_نقش_SuperAdmin_بدون_access_manage_رد_می‌شود()
    {
        Assert.Throws<CommandValidationException>(() =>
            SuperAdminRoleAssignmentGuard.EnsureAssignable(targetRoleSetIncludesSuperAdmin: true, actorHasAccessManage: false));
    }

    [Fact]
    public void انتساب_نقش_SuperAdmin_با_access_manage_مجاز_است()
    {
        var exception = Record.Exception(() =>
            SuperAdminRoleAssignmentGuard.EnsureAssignable(targetRoleSetIncludesSuperAdmin: true, actorHasAccessManage: true));

        Assert.Null(exception);
    }

    [Fact]
    public void انتساب_نقش‌های_غیر_SuperAdmin_بدون_access_manage_هم_مجاز_است()
    {
        var exception = Record.Exception(() =>
            SuperAdminRoleAssignmentGuard.EnsureAssignable(targetRoleSetIncludesSuperAdmin: false, actorHasAccessManage: false));

        Assert.Null(exception);
    }
}
