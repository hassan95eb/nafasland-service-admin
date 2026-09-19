using Microsoft.AspNetCore.Http;

namespace NafasLand.Admin.Shared.Infrastructure.CorrelationId;

internal sealed class HttpCorrelationIdAccessor(IHttpContextAccessor httpContextAccessor) : ICorrelationIdAccessor
{
    public string CorrelationId =>
        httpContextAccessor.HttpContext?.Items[CorrelationIdMiddleware.ItemsKey] as string
        ?? "بدون-correlation-id";
}
