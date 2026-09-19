using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace NafasLand.Admin.Shared.Infrastructure.CorrelationId;

/// <summary>
/// شناسهٔ کاربر را در LogContext منتشر می‌کند (ADR-042). باید بعد از
/// <c>UseAuthentication</c> ثبت شود تا claims کاربر آماده باشد.
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
