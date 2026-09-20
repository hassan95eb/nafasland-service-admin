using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Features.ToggleUserActive;

internal sealed record ToggleUserActiveCommand(Guid TargetUserId)
    : ICommand<ToggleUserActiveResult>, IRequiresPermission, IAuditableCommand
{
    public string RequiredPermission => IdentityPermissions.UsersManage;
    public string AuditAction => "UserActiveStatusChanged";
    public string AuditEntityType => "User";
}

internal sealed record ToggleUserActiveResult(bool IsActive);
