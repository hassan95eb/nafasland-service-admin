using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.ToggleUserActive;

internal static class ToggleUserActiveEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/identity/users/{id:guid}/toggle-active", async (
                Guid id,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var result = await dispatcher.SendAsync<ToggleUserActiveCommand, ToggleUserActiveResult>(
                    new ToggleUserActiveCommand(id),
                    cancellationToken);
                return Results.Ok(result);
            })
            .RequireAuthorization();
    }
}
