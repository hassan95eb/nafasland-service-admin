using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Infrastructure;

internal sealed class PortalRateLimiter : IAsyncDisposable
{
    private readonly TokenBucketRateLimiter _limiter;
    private readonly TimeSpan _queueTimeout;

    public PortalRateLimiter(IOptions<PortalOptions> options)
        : this(options.Value.RateLimitPerSecond, options.Value.RateLimitQueueCapacity,
            TimeSpan.FromSeconds(options.Value.RateLimitQueueTimeoutSeconds))
    {
    }

    internal PortalRateLimiter(double permitsPerSecond, int queueCapacity, TimeSpan queueTimeout)
    {
        _queueTimeout = queueTimeout;
        _limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 1,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = queueCapacity,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1d / permitsPerSecond),
            TokensPerPeriod = 1,
            AutoReplenishment = true,
        });
    }

    public async ValueTask<RateLimitLease> AcquireAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_queueTimeout);

        try
        {
            var lease = await _limiter.AcquireAsync(1, timeout.Token);
            if (!lease.IsAcquired)
            {
                lease.Dispose();
                throw new PortalBusyException();
            }

            return lease;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PortalBusyException();
        }
    }

    public ValueTask DisposeAsync() => _limiter.DisposeAsync();
}
