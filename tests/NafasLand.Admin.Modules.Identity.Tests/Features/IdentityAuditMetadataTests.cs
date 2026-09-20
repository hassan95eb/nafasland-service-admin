using NafasLand.Admin.Modules.Identity.Features.CreateUser;
using NafasLand.Admin.Modules.Identity.Features.ResetPassword;
using NafasLand.Admin.Modules.Identity.Features.SetRolePermissions;
using NafasLand.Admin.Modules.Identity.Features.SetUserPermission;
using NafasLand.Admin.Modules.Identity.Features.SetUserRoles;
using NafasLand.Admin.Modules.Identity.Features.ToggleUserActive;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Identity.Tests.Features;

public sealed class IdentityAuditMetadataTests
{
    public static TheoryData<IAuditableCommand, string, string> Commands => new()
    {
        { new CreateUserCommand("new-admin", "temporary-password"), "UserCreated", "User" },
        { new ResetPasswordCommand(Guid.NewGuid(), "temporary-password"), "UserPasswordReset", "User" },
        { new ToggleUserActiveCommand(Guid.NewGuid()), "UserActiveStatusChanged", "User" },
        { new SetUserRolesCommand(Guid.NewGuid(), []), "UserRolesChanged", "User" },
        { new SetUserPermissionCommand(Guid.NewGuid(), "sample.ping", PermissionEffect.Grant), "UserPermissionChanged", "User" },
        { new SetRolePermissionsCommand(Guid.NewGuid(), []), "RolePermissionsChanged", "Role" },
    };

    [Theory]
    [MemberData(nameof(Commands))]
    public void عملیات_مدیریتی_متادیتای_Audit_دارند(
        IAuditableCommand command,
        string expectedAction,
        string expectedEntityType)
    {
        Assert.Equal(expectedAction, command.AuditAction);
        Assert.Equal(expectedEntityType, command.AuditEntityType);
    }
}
