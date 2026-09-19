using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Features.SetRolePermissions;

/// <summary>Replaces the role's full permission set (PUT semantics), not an add/remove diff.</summary>
internal sealed record SetRolePermissionsCommand(Guid RoleId, IReadOnlyCollection<string> PermissionKeys)
    : ICommand<SetRolePermissionsResult>, IRequiresPermission
{
    public string RequiredPermission => IdentityPermissions.AccessManage;
}

internal sealed record SetRolePermissionsResult;
