using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.SetRolePermissions;

internal static class SetRolePermissionsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/identity/roles/{roleId:guid}/permissions", async (
                Guid roleId,
                SetRolePermissionsRequest request,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var command = new SetRolePermissionsCommand(roleId, request.PermissionKeys);
                await dispatcher.SendAsync<SetRolePermissionsCommand, SetRolePermissionsResult>(command, cancellationToken);
                return Results.Ok();
            })
            .RequireAuthorization();
    }
}

internal sealed record SetRolePermissionsRequest(IReadOnlyCollection<string> PermissionKeys);
