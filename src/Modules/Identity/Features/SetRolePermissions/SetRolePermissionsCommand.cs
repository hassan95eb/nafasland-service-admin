using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Features.SetRolePermissions;

/// <summary>Replaces the role's full permission set (PUT semantics), not an add/remove diff.</summary>
internal sealed record SetRolePermissionsCommand(Guid RoleId, IReadOnlyCollection<string> PermissionKeys)
    : ICommand<SetRolePermissionsResult>, IRequiresPermission, IAuditableCommand
{
    public string RequiredPermission => IdentityPermissions.AccessManage;
    public string AuditAction => "RolePermissionsChanged";
    public string AuditEntityType => "Role";
}

internal sealed record SetRolePermissionsResult;
