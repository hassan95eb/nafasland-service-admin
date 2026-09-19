using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace NafasLand.Admin.Shared.Infrastructure.CorrelationId;

/// <summary>
/// Publishes the user id into LogContext (ADR-042). Must be registered after
/// <c>UseAuthentication</c> so the user's claims are ready.
/// </summary>
internal sealed class UserContextLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

        using (LogContext.PushProperty("UserId", userId))
        {
            await next(context);
        }
    }
}
