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
                    .Select(u => new UserDetailsResponse(
                        u.Id,
                        u.Username,
                        u.IsActive,
                        u.IsProtected,
                        u.MustChangePassword,
                        u.CreatedAt,
                        u.UserRoles.Select(userRole => userRole.Role!.Name).ToArray(),
                        u.UserPermissions.Select(userPermission => new DirectPermissionResponse(
                            userPermission.Permission!.Key,
                            userPermission.Effect.ToString())).ToArray()))
                    .SingleOrDefaultAsync(cancellationToken);

                return user is null ? Results.NotFound() : Results.Ok(user);
            })
            .Produces<UserDetailsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequirePermission(IdentityPermissions.UsersManage);
    }
}

internal sealed record UserDetailsResponse(
    Guid Id,
    string Username,
    bool IsActive,
    bool IsProtected,
    bool MustChangePassword,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<DirectPermissionResponse> DirectPermissions);

internal sealed record DirectPermissionResponse(string Key, string Effect);
