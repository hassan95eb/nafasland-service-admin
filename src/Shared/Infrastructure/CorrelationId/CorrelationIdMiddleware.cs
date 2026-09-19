using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace NafasLand.Admin.Shared.Infrastructure.CorrelationId;

/// <summary>
/// Reads the correlation id from the <c>X-Correlation-Id</c> header or creates one,
/// publishes it into Serilog's LogContext (ADR-036), and returns it in the
/// response header.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemsKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
                ? existing.ToString()
                : Guid.NewGuid().ToString("N");

        context.Items[ItemsKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
