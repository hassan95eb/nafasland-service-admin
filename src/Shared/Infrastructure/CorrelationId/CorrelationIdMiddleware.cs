using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace NafasLand.Admin.Shared.Infrastructure.CorrelationId;

/// <summary>
/// CorrelationId را از هدر <c>X-Correlation-Id</c> می‌خواند یا می‌سازد، در
/// LogContext سریلاگ منتشر می‌کند (ADR-036) و در هدر پاسخ برمی‌گرداند.
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
