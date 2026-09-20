using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Features.SetUserPermission;

/// <summary>Effect null removes any existing direct Grant/Deny for this (user, permission) pair.</summary>
internal sealed record SetUserPermissionCommand(Guid TargetUserId, string PermissionKey, PermissionEffect? Effect)
    : ICommand<SetUserPermissionResult>, IRequiresPermission, IAuditableCommand
{
    public string RequiredPermission => IdentityPermissions.AccessManage;
    public string AuditAction => "UserPermissionChanged";
    public string AuditEntityType => "User";
}

internal sealed record SetUserPermissionResult;
