using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Identity.Configuration;
using NafasLand.Admin.Modules.Identity.Contracts;
using NafasLand.Admin.Modules.Identity.Security;

namespace NafasLand.Admin.Modules.Identity.Persistence;

/// <summary>
/// Implements the startup bootstrap described in the step-01 prompt: sync the
/// Permission table from every module's declarations (never deleting a row, even
/// for a currently-disabled module — ADR-005), ensure Admin (no permissions) and
/// SuperAdmin (system-managed) roles exist, keep SuperAdmin filled with every
/// known permission, and seed the first SuperAdmin account (ADR-022).
/// </summary>
internal sealed class IdentityBootstrapper(
    IdentityDbContext dbContext,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    IOptions<SuperAdminSeedOptions> superAdminOptions) : IIdentityBootstrapper
{
    public async Task BootstrapAsync(IReadOnlyList<ModulePermissionDefinition> allPermissions, CancellationToken cancellationToken)
    {
        await SynchronizePermissionsAsync(allPermissions, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await EnsureRoleAsync(IdentityRoles.Admin, isSystemManaged: false, cancellationToken);
        var superAdminRole = await EnsureRoleAsync(IdentityRoles.SuperAdmin, isSystemManaged: true, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await FillSuperAdminRoleWithAllPermissionsAsync(superAdminRole, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedSuperAdminUserAsync(superAdminRole, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SynchronizePermissionsAsync(IReadOnlyList<ModulePermissionDefinition> allPermissions, CancellationToken cancellationToken)
    {
        var existingByKey = await dbContext.Permissions.ToDictionaryAsync(permission => permission.Key, cancellationToken);

        foreach (var (moduleName, definition) in allPermissions)
        {
            if (existingByKey.TryGetValue(definition.Key, out var existing))
            {
                existing.SyncFrom(definition.DisplayName, moduleName, definition.IsSuperAdminOnly);
                continue;
            }

            dbContext.Permissions.Add(Permission.Create(
                definition.Key,
                definition.DisplayName,
                moduleName,
                definition.IsSuperAdminOnly));
        }
    }

    private async Task<Role> EnsureRoleAsync(string name, bool isSystemManaged, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        if (role is not null)
        {
            return role;
        }

        role = Role.Create(name, isSystemManaged);
        dbContext.Roles.Add(role);
        return role;
    }

    private async Task FillSuperAdminRoleWithAllPermissionsAsync(Role superAdminRole, CancellationToken cancellationToken)
    {
        var allPermissionIds = await dbContext.Permissions.Select(permission => permission.Id).ToListAsync(cancellationToken);
        var alreadyAssignedIds = await dbContext.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == superAdminRole.Id)
            .Select(rolePermission => rolePermission.PermissionId)
            .ToListAsync(cancellationToken);

        var missingIds = allPermissionIds.Except(alreadyAssignedIds);
        foreach (var permissionId in missingIds)
        {
            dbContext.RolePermissions.Add(new RolePermission(superAdminRole.Id, permissionId));
        }
    }

    private async Task SeedSuperAdminUserAsync(Role superAdminRole, CancellationToken cancellationToken)
    {
        var anySuperAdminExists = await dbContext.UserRoles
            .AnyAsync(userRole => userRole.RoleId == superAdminRole.Id, cancellationToken);
        if (anySuperAdminExists)
        {
            return;
        }

        var options = superAdminOptions.Value;
        var passwordHash = passwordHasher.Hash(options.Password);
        var user = AppUser.Create(
            options.Username,
            passwordHash,
            passwordHasher.Algorithm,
            timeProvider.GetUtcNow(),
            mustChangePassword: true,
            isProtected: true,
            createdByUserId: null);

        dbContext.Users.Add(user);
        dbContext.UserRoles.Add(new UserRole(user.Id, superAdminRole.Id));
    }
}
