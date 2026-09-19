using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Identity.Features.Queries;

internal static class ListPermissionsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/identity/permissions", async (IdentityDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var permissions = await dbContext.Permissions
                    .AsNoTracking()
                    .OrderBy(permission => permission.ModuleName).ThenBy(permission => permission.Key)
                    .Select(permission => new
                    {
                        permission.Key,
                        permission.ModuleName,
                        permission.IsSuperAdminOnly,
                    })
                    .ToListAsync(cancellationToken);

                return Results.Ok(permissions);
            })
            .RequireAuthorization()
            .RequirePermission(IdentityPermissions.AccessManage);
    }
}
