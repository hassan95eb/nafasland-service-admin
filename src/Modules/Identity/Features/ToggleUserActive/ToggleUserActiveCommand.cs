using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Features.ToggleUserActive;

internal sealed record ToggleUserActiveCommand(Guid TargetUserId) : ICommand<ToggleUserActiveResult>, IRequiresPermission
{
    public string RequiredPermission => IdentityPermissions.UsersManage;
}

internal sealed record ToggleUserActiveResult(bool IsActive);
