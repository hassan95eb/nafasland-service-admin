using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.SetUserPermission;

internal static class SetUserPermissionEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/identity/users/{id:guid}/permissions/{permissionKey}", async (
                Guid id,
                string permissionKey,
                SetUserPermissionRequest request,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                PermissionEffect? effect = request.Effect switch
                {
                    null => null,
                    "Grant" => PermissionEffect.Grant,
                    "Deny" => PermissionEffect.Deny,
                    _ => throw new CommandValidationException(new Dictionary<string, string[]>
                    {
                        [nameof(request.Effect)] = [$"مقدار effect باید Grant، Deny یا null باشد؛ مقدار دریافتی: {request.Effect}"],
                    }),
                };

                var command = new SetUserPermissionCommand(id, permissionKey, effect);
                await dispatcher.SendAsync<SetUserPermissionCommand, SetUserPermissionResult>(command, cancellationToken);
                return Results.Ok();
            })
            .RequireAuthorization();
    }
}

internal sealed record SetUserPermissionRequest(string? Effect);
