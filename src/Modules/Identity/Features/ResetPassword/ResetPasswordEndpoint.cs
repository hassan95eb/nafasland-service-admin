using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.ResetPassword;

internal static class ResetPasswordEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/identity/users/{id:guid}/reset-password", async (
                Guid id,
                ResetPasswordRequest request,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var command = new ResetPasswordCommand(id, request.NewPassword);
                await dispatcher.SendAsync<ResetPasswordCommand, ResetPasswordResult>(command, cancellationToken);
                return Results.Ok();
            })
            .RequireAuthorization();
    }
}

internal sealed record ResetPasswordRequest(string NewPassword);
