using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Shared.Infrastructure.Portal;

/// <summary>
/// The single process-wide limiter in front of every portal call (ADR-026):
/// the portal's 2 req/s ceiling is per IP, so every module's HttpClient shares
/// this one singleton — see <see cref="PortalHttpClientExtensions"/>.
/// </summary>
internal sealed class PortalRateLimiter : IAsyncDisposable
{
    private readonly TokenBucketRateLimiter _limiter;
    private readonly TimeSpan _queueTimeout;

    public PortalRateLimiter(IOptions<PortalConnectionOptions> options)
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
