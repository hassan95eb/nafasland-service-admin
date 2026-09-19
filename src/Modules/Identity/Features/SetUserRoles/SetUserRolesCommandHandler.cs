using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Modules.Identity.Security;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.SetUserRoles;

internal sealed class SetUserRolesCommandHandler(IdentityDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    : ICommandHandler<SetUserRolesCommand, SetUserRolesResult>
{
    public async Task<SetUserRolesResult> HandleAsync(SetUserRolesCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleAsync(u => u.Id == command.TargetUserId, cancellationToken);
        user.EnsureNotProtected("تغییر نقش‌ها");

        var distinctRoleIds = command.RoleIds.Distinct().ToList();

        // ADR-046: assigning the SuperAdmin role is itself a SuperAdmin-only
        // action, separate from (and in addition to) identity.users.manage
        // which already gates this whole command.
        var targetRoleSetIncludesSuperAdmin = await dbContext.Roles
            .Where(role => distinctRoleIds.Contains(role.Id))
            .AnyAsync(role => role.IsSystemManaged, cancellationToken);

        var actorHasAccessManage = httpContextAccessor.HttpContext?.User
            .HasClaim(PermissionClaimTypes.Permission, IdentityPermissions.AccessManage) ?? false;

        SuperAdminRoleAssignmentGuard.EnsureAssignable(targetRoleSetIncludesSuperAdmin, actorHasAccessManage);

        var currentUserRoles = await dbContext.UserRoles
            .Where(userRole => userRole.UserId == command.TargetUserId)
            .ToListAsync(cancellationToken);
        dbContext.UserRoles.RemoveRange(currentUserRoles);

        foreach (var roleId in distinctRoleIds)
        {
            dbContext.UserRoles.Add(new UserRole(command.TargetUserId, roleId));
        }

        return new SetUserRolesResult();
    }
}
