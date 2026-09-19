using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Identity.Features.Queries;

internal static class ListUsersEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/identity/users", async (IdentityDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var users = await dbContext.Users
                    .AsNoTracking()
                    .OrderBy(user => user.Username)
                    .Select(user => new
                    {
                        user.Id,
                        user.Username,
                        user.IsActive,
                        user.IsProtected,
                        user.MustChangePassword,
                        Roles = user.UserRoles.Select(userRole => userRole.Role!.Name),
                    })
                    .ToListAsync(cancellationToken);

                return Results.Ok(users);
            })
            .RequireAuthorization()
            .RequirePermission(IdentityPermissions.UsersManage);
    }
}
