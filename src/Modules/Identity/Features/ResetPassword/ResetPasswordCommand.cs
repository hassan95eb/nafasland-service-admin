using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Features.ResetPassword;

internal sealed record ResetPasswordCommand(Guid TargetUserId, string NewPassword)
    : ICommand<ResetPasswordResult>, IRequiresPermission, IAuditableCommand
{
    public string RequiredPermission => IdentityPermissions.UsersManage;
    public string AuditAction => "UserPasswordReset";
    public string AuditEntityType => "User";
}

internal sealed record ResetPasswordResult;
