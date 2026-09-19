using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Contracts;
using NafasLand.Admin.Modules.Identity.Security;

namespace NafasLand.Admin.Modules.Identity.Persistence;

/// <summary>
/// Real, database-backed implementation of <see cref="IEffectivePermissionsProvider"/>.
/// The actual union/grant/deny rule lives in <see cref="EffectivePermissionCalculator"/>
/// so it can be unit tested without a database; this class only fetches the three
/// key sets ADR-021 needs.
/// </summary>
internal sealed class EffectivePermissionsProvider(IdentityDbContext dbContext) : IEffectivePermissionsProvider
{
    public async Task<EffectiveUserAccess?> GetEffectiveAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.MustChangePassword })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var rolePermissionKeys = await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .SelectMany(userRole => userRole.Role!.RolePermissions.Select(rolePermission => rolePermission.Permission!.Key))
            .Distinct()
            .ToListAsync(cancellationToken);

        var grantedKeys = await dbContext.UserPermissions
            .AsNoTracking()
            .Where(userPermission => userPermission.UserId == userId && userPermission.Effect == PermissionEffect.Grant)
            .Select(userPermission => userPermission.Permission!.Key)
            .ToListAsync(cancellationToken);

        var deniedKeys = await dbContext.UserPermissions
            .AsNoTracking()
            .Where(userPermission => userPermission.UserId == userId && userPermission.Effect == PermissionEffect.Deny)
            .Select(userPermission => userPermission.Permission!.Key)
            .ToListAsync(cancellationToken);

        var effective = EffectivePermissionCalculator.Calculate(rolePermissionKeys, grantedKeys, deniedKeys);
        return new EffectiveUserAccess(effective, user.MustChangePassword);
    }
}
