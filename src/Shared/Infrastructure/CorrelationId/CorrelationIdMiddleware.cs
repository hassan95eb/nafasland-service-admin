using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace NafasLand.Admin.Shared.Infrastructure.CorrelationId;

/// <summary>
/// Reads the correlation id from the <c>X-Correlation-Id</c> header or creates one,
/// publishes it into Serilog's LogContext (ADR-036), and returns it in the
/// response header. An incoming value is only trusted when it is at most
/// <see cref="MaxIncomingLength"/> characters of [A-Za-z0-9-_]; anything else is
/// replaced, since it is echoed back, logged, and stored in AuditLog.CorrelationId.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemsKey = "CorrelationId";
    public const int MaxIncomingLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            && IsAcceptable(existing.ToString())
                ? existing.ToString()
                : Guid.NewGuid().ToString("N");

        context.Items[ItemsKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    internal static bool IsAcceptable(string value) =>
        value.Length is > 0 and <= MaxIncomingLength
        && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
