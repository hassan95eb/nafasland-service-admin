using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.ChangePassword;

internal static class ChangePasswordEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/identity/auth/change-password", async (
                ChangePasswordRequest request,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);
                await dispatcher.SendAsync<ChangePasswordCommand, ChangePasswordResult>(command, cancellationToken);
                return Results.Ok();
            })
            .RequireAuthorization();
    }
}

internal sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
