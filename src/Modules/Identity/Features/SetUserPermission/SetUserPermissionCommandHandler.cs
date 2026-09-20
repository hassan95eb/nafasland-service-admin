using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Modules.Identity.Security;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.SetUserPermission;

internal sealed class SetUserPermissionCommandHandler(IdentityDbContext dbContext, IAuditContext auditContext)
    : ICommandHandler<SetUserPermissionCommand, SetUserPermissionResult>
{
    public async Task<SetUserPermissionResult> HandleAsync(SetUserPermissionCommand command, CancellationToken cancellationToken)
    {
        auditContext.SetEntityId(command.TargetUserId.ToString());
        var permission = await dbContext.Permissions.SingleAsync(p => p.Key == command.PermissionKey, cancellationToken);

        if (command.Effect == PermissionEffect.Grant)
        {
            var targetIsSuperAdmin = await dbContext.UserRoles
                .Where(userRole => userRole.UserId == command.TargetUserId)
                .Join(dbContext.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name)
                .AnyAsync(roleName => roleName == IdentityRoles.SuperAdmin, cancellationToken);

            SuperAdminOnlyPermissionGuard.EnsureGrantAllowed(permission.Key, permission.IsSuperAdminOnly, targetIsSuperAdmin);
        }

        var existing = await dbContext.UserPermissions
            .SingleOrDefaultAsync(
                userPermission => userPermission.UserId == command.TargetUserId && userPermission.PermissionId == permission.Id,
                cancellationToken);
        auditContext.SetBefore(new { command.PermissionKey, Effect = existing?.Effect.ToString() });

        if (command.Effect is null)
        {
            if (existing is not null)
            {
                dbContext.UserPermissions.Remove(existing);
            }

            auditContext.SetAfter(new { command.PermissionKey, Effect = (string?)null });

            return new SetUserPermissionResult();
        }

        if (existing is not null)
        {
            existing.SetEffect(command.Effect.Value);
        }
        else
        {
            dbContext.UserPermissions.Add(new UserPermission(command.TargetUserId, permission.Id, command.Effect.Value));
        }

        auditContext.SetAfter(new { command.PermissionKey, Effect = command.Effect.Value.ToString() });

        return new SetUserPermissionResult();
    }
}
