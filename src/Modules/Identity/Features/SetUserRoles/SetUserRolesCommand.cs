using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Features.SetUserRoles;

/// <summary>Replaces the target user's full role set (PUT semantics), not an add/remove diff.</summary>
internal sealed record SetUserRolesCommand(Guid TargetUserId, IReadOnlyCollection<Guid> RoleIds)
    : ICommand<SetUserRolesResult>, IRequiresPermission
{
    public string RequiredPermission => IdentityPermissions.UsersManage;
}

internal sealed record SetUserRolesResult;
