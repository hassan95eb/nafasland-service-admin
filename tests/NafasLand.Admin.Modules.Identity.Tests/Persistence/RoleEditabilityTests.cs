using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Identity.Tests.Persistence;

/// <summary>SetRolePermissionsCommandHandler calls Role.EnsureEditable() before touching a role's permissions; this is the required "reject SetRolePermissions on IsSystemManaged" test, at the entity level (no database needed).</summary>
public sealed class RoleEditabilityTests
{
    [Fact]
    public void نقش_IsSystemManaged_قابل_ویرایش_نیست()
    {
        var superAdmin = Role.Create("SuperAdmin", isSystemManaged: true);

        Assert.Throws<ConflictException>(() => superAdmin.EnsureEditable());
    }

    [Fact]
    public void نقش_معمولی_قابل_ویرایش_است()
    {
        var admin = Role.Create("Admin", isSystemManaged: false);

        var exception = Record.Exception(() => admin.EnsureEditable());

        Assert.Null(exception);
    }
}
