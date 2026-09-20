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
                    .Select(user => new UserListItemResponse(
                        user.Id,
                        user.Username,
                        user.IsActive,
                        user.IsProtected,
                        user.MustChangePassword,
                        user.UserRoles.Select(userRole => userRole.Role!.Name).ToArray()))
                    .ToListAsync(cancellationToken);

                return TypedResults.Ok(users);
            })
            .RequireAuthorization()
            .RequirePermission(IdentityPermissions.UsersManage);
    }
}

internal sealed record UserListItemResponse(
    Guid Id,
    string Username,
    bool IsActive,
    bool IsProtected,
    bool MustChangePassword,
    IReadOnlyCollection<string> Roles);
