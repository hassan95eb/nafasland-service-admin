namespace NafasLand.Admin.Shared.Infrastructure.Portal;

internal sealed class PortalRateLimitingHandler(PortalRateLimiter rateLimiter) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var lease = await rateLimiter.AcquireAsync(cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
