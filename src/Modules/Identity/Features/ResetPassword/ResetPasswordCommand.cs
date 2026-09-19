using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Features.ResetPassword;

internal sealed record ResetPasswordCommand(Guid TargetUserId, string NewPassword)
    : ICommand<ResetPasswordResult>, IRequiresPermission
{
    public string RequiredPermission => IdentityPermissions.UsersManage;
}

internal sealed record ResetPasswordResult;
