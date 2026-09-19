using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.SetUserRoles;

internal static class SetUserRolesEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/identity/users/{id:guid}/roles", async (
                Guid id,
                SetUserRolesRequest request,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var command = new SetUserRolesCommand(id, request.RoleIds);
                await dispatcher.SendAsync<SetUserRolesCommand, SetUserRolesResult>(command, cancellationToken);
                return Results.Ok();
            })
            .RequireAuthorization();
    }
}

internal sealed record SetUserRolesRequest(IReadOnlyCollection<Guid> RoleIds);
