using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Tests.Infrastructure;

public sealed class PortalRateLimiterTests
{
    [Fact]
    public async Task فراخوانی‌های_مازاد_در_صف_منتظر_می‌مانند_و_بعد_اجرا_می‌شوند()
    {
        await using var limiter = new PortalRateLimiter(2, 4, TimeSpan.FromSeconds(3));

        var acquisitions = Enumerable.Range(0, 4)
            .Select(_ => limiter.AcquireAsync(CancellationToken.None).AsTask())
            .ToArray();

        await Task.Delay(100);
        Assert.Equal(1, acquisitions.Count(task => task.IsCompletedSuccessfully));

        var leases = await Task.WhenAll(acquisitions).WaitAsync(TimeSpan.FromSeconds(3));
        foreach (var lease in leases)
        {
            lease.Dispose();
        }
    }

    [Fact]
    public async Task صف_پر_بلافاصله_با_خطای_سامانه_شلوغ_رد_می‌شود()
    {
        await using var limiter = new PortalRateLimiter(0.5, 1, TimeSpan.FromSeconds(5));
        using var first = await limiter.AcquireAsync(CancellationToken.None);
        using var queuedCancellation = new CancellationTokenSource();
        var queued = limiter.AcquireAsync(queuedCancellation.Token).AsTask();
        await Task.Yield();

        await Assert.ThrowsAsync<PortalBusyException>(
            () => limiter.AcquireAsync(CancellationToken.None).AsTask());

        queuedCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
    }

    [Fact]
    public async Task پایان_مهلت_انتظار_صف_به_خطای_سامانه_شلوغ_تبدیل_می‌شود()
    {
        await using var limiter = new PortalRateLimiter(0.1, 1, TimeSpan.FromMilliseconds(50));
        using var first = await limiter.AcquireAsync(CancellationToken.None);

        await Assert.ThrowsAsync<PortalBusyException>(
            () => limiter.AcquireAsync(CancellationToken.None).AsTask());
    }
}
