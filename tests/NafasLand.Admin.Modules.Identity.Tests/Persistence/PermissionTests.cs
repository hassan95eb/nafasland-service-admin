using NafasLand.Admin.Modules.Identity.Persistence;

namespace NafasLand.Admin.Modules.Identity.Tests.Persistence;

public sealed class PermissionTests
{
    [Fact]
    public void Create_برچسب_نمایشی_را_نگه_می‌دارد()
    {
        var permission = Permission.Create("identity.users.manage", "مدیریت کاربران", "Identity", false);

        Assert.Equal("مدیریت کاربران", permission.DisplayName);
    }

    [Fact]
    public void SyncFrom_برچسب_نمایشی_را_به‌روز_می‌کند()
    {
        var permission = Permission.Create("identity.users.manage", "قدیمی", "Identity", false);

        permission.SyncFrom("مدیریت کاربران", "Identity", true);

        Assert.Equal("مدیریت کاربران", permission.DisplayName);
        Assert.True(permission.IsSuperAdminOnly);
    }
}
