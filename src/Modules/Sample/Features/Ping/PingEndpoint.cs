using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Sample.Features.Ping;

internal static class PingEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/sample/ping", async (
            PingRequest request,
            ICommandDispatcher dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new PingCommand(request.Message);
            var result = await dispatcher.SendAsync<PingCommand, PingResult>(command, cancellationToken);
            return Results.Ok(result);
        });
    }
}

internal sealed record PingRequest(string Message);
