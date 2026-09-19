using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace NafasLand.Admin.Shared.Infrastructure.Authorization;

/// <summary>
/// For the "simpler than Command/Handler" read-only endpoints (queries) that
/// prompt-01 allows to read straight from a DbContext: they still need a
/// permission check, just not the full mandatory pipeline (ADR-006 only applies
/// to mutating commands). This gives them the same permission check as
/// AuthorizationBehavior without going through ICommand.
/// </summary>
public static class PermissionEndpointExtensions
{
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated is not true || !user.HasClaim(PermissionClaimTypes.Permission, permission))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "دسترسی غیرمجاز",
                    detail: $"دسترسی {permission} لازم است.");
            }

            return await next(context);
        });
    }
}
