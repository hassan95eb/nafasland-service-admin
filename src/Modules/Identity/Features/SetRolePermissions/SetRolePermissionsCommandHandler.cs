using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Modules.Identity.Security;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.SetRolePermissions;

internal sealed class SetRolePermissionsCommandHandler(IdentityDbContext dbContext, IAuditContext auditContext)
    : ICommandHandler<SetRolePermissionsCommand, SetRolePermissionsResult>
{
    public async Task<SetRolePermissionsResult> HandleAsync(SetRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.SingleAsync(r => r.Id == command.RoleId, cancellationToken);
        auditContext.SetEntityId(role.Id.ToString());
        role.EnsureEditable();

        var distinctKeys = command.PermissionKeys.Distinct().ToList();
        var permissions = await dbContext.Permissions
            .Where(permission => distinctKeys.Contains(permission.Key))
            .ToListAsync(cancellationToken);

        // role.EnsureEditable() above already guarantees this role is not
        // SuperAdmin (the only system-managed role), so no requested permission
        // may be SuperAdminOnly.
        foreach (var permission in permissions)
        {
            SuperAdminOnlyPermissionGuard.EnsureGrantAllowed(permission.Key, permission.IsSuperAdminOnly, targetIsSuperAdmin: false);
        }

        var currentRolePermissions = await dbContext.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == command.RoleId)
            .ToListAsync(cancellationToken);
        var currentPermissionIds = currentRolePermissions.Select(item => item.PermissionId).ToArray();
        var currentPermissionKeys = await dbContext.Permissions
            .Where(permission => currentPermissionIds.Contains(permission.Id))
            .Select(permission => permission.Key)
            .OrderBy(key => key)
            .ToArrayAsync(cancellationToken);
        auditContext.SetBefore(new { PermissionKeys = currentPermissionKeys });
        dbContext.RolePermissions.RemoveRange(currentRolePermissions);

        foreach (var permission in permissions)
        {
            dbContext.RolePermissions.Add(new RolePermission(command.RoleId, permission.Id));
        }

        auditContext.SetAfter(new { PermissionKeys = permissions.Select(permission => permission.Key).Order().ToArray() });

        return new SetRolePermissionsResult();
    }
}
