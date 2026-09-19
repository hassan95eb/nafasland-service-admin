using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Sample.Features.UnprotectedPing;

internal static class UnprotectedPingEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/sample/unprotected-ping", async (
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync<UnprotectedPingCommand, UnprotectedPingResult>(
                new UnprotectedPingCommand(),
                cancellationToken);
            return Results.Ok(result);
        });
    }
}
