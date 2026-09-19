using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Shared.Infrastructure.Antiforgery;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.Login;

internal static class LoginEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/identity/auth/login", async (
                LoginRequest request,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var command = new LoginCommand(request.Username, request.Password);
                var result = await dispatcher.SendAsync<LoginCommand, LoginResult>(command, cancellationToken);
                return Results.Ok(result);
            })
            .AllowAnonymous()
            // No session cookie exists yet at login time, so there is nothing to
            // validate an antiforgery token against.
            .SkipAntiforgeryValidation();
    }
}

internal sealed record LoginRequest(string Username, string Password);
