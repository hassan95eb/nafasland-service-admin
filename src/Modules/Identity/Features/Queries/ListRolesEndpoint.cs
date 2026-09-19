using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Identity.Features.Queries;

internal static class ListRolesEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/identity/roles", async (IdentityDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var roles = await dbContext.Roles
                    .AsNoTracking()
                    .OrderBy(role => role.Name)
                    .Select(role => new
                    {
                        role.Id,
                        role.Name,
                        role.IsSystemManaged,
                        PermissionKeys = role.RolePermissions.Select(rolePermission => rolePermission.Permission!.Key),
                    })
                    .ToListAsync(cancellationToken);

                return Results.Ok(roles);
            })
            .RequireAuthorization()
            .RequirePermission(IdentityPermissions.AccessManage);
    }
}
