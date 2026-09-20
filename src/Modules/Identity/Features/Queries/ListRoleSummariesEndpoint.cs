using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Identity.Features.Queries;

internal static class ListRoleSummariesEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/identity/roles/summary", async (
                IdentityDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var roles = await dbContext.Roles
                    .AsNoTracking()
                    .OrderBy(role => role.Name)
                    .Select(role => new RoleSummaryResponse(role.Id, role.Name, role.IsSystemManaged))
                    .ToListAsync(cancellationToken);

                return TypedResults.Ok(roles);
            })
            .RequireAuthorization()
            .RequirePermission(IdentityPermissions.UsersManage);
    }
}

internal sealed record RoleSummaryResponse(Guid Id, string Name, bool IsSystemManaged);
