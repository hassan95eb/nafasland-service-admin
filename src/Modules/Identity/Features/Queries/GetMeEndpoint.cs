using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Modules.Identity.Security;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Identity.Features.Queries;

/// <summary>
/// Only needs a signed-in user, no specific permission — simpler than the
/// Command/Handler pattern (prompt-01: queries may read straight from the
/// DbContext when there is nothing to audit and no permission beyond "is this
/// user logged in").
/// </summary>
internal static class GetMeEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/identity/auth/me", async (
                HttpContext httpContext,
                IdentityDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var userId = CurrentUserAccessor.GetUserId(httpContext.User);
                var user = await dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.Id, u.Username, u.MustChangePassword, u.IsActive })
                    .SingleAsync(cancellationToken);

                var roleNames = await dbContext.UserRoles
                    .AsNoTracking()
                    .Where(userRole => userRole.UserId == userId)
                    .Select(userRole => userRole.Role!.Name)
                    .ToListAsync(cancellationToken);

                var permissions = httpContext.User
                    .FindAll(PermissionClaimTypes.Permission)
                    .Select(claim => claim.Value)
                    .ToList();

                return TypedResults.Ok(new MeResponse(
                    user.Id,
                    user.Username,
                    user.MustChangePassword,
                    user.IsActive,
                    roleNames,
                    permissions));
            })
            .RequireAuthorization();
    }
}

internal sealed record MeResponse(
    Guid Id,
    string Username,
    bool MustChangePassword,
    bool IsActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
