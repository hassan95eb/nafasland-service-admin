using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.Logout;

internal static class LogoutEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/identity/auth/logout", async (
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                await dispatcher.SendAsync<LogoutCommand, LogoutResult>(new LogoutCommand(), cancellationToken);
                return Results.Ok();
            })
            .RequireAuthorization();
    }
}
