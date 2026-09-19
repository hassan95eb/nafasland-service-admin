using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Identity.Features.Queries;

internal static class GetUserEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/identity/users/{id:guid}", async (
                Guid id,
                IdentityDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var user = await dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Id == id)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.IsActive,
                        u.IsProtected,
                        u.MustChangePassword,
                        u.CreatedAt,
                        Roles = u.UserRoles.Select(userRole => userRole.Role!.Name),
                        DirectPermissions = u.UserPermissions.Select(userPermission => new
                        {
                            Key = userPermission.Permission!.Key,
                            Effect = userPermission.Effect.ToString(),
                        }),
                    })
                    .SingleOrDefaultAsync(cancellationToken);

                return user is null ? Results.NotFound() : Results.Ok(user);
            })
            .RequireAuthorization()
            .RequirePermission(IdentityPermissions.UsersManage);
    }
}
