using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Tests.Infrastructure;

public sealed class ProductCacheTests
{
    [Fact]
    public async Task فهرست_منقضی_هنگام_قطعی_با_پرچم_کهنه_برمی‌گردد()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.Zero));
        var cache = new ProductCache(clock);
        var query = new PortalProductListQuery(1, 25, null, null);
        var source = new PortalProductListResult([], 0, 0, 1, 25);

        var fresh = await cache.GetListAsync(query, _ => Task.FromResult(source), CancellationToken.None);
        clock.Advance(TimeSpan.FromMinutes(2));
        var stale = await cache.GetListAsync(
            query,
            _ => throw new PortalUnavailableException(),
            CancellationToken.None);

        Assert.False(fresh.IsStale);
        Assert.True(stale.IsStale);
        Assert.Equal(fresh.AsOfUtc, stale.AsOfUtc);
    }

    [Fact]
    public async Task جزئیات_منقضی_هنگام_قطعی_با_پرچم_کهنه_برمی‌گردد()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.Zero));
        var cache = new ProductCache(clock);
        var detail = FakePortalProductClient.CreateDetail();

        var fresh = await cache.GetDetailAsync("101", _ => Task.FromResult<PortalProductDetail?>(detail), CancellationToken.None);
        clock.Advance(TimeSpan.FromMinutes(2));
        var stale = await cache.GetDetailAsync(
            "101",
            _ => throw new PortalUnavailableException(),
            CancellationToken.None);

        Assert.NotNull(fresh);
        Assert.NotNull(stale);
        Assert.False(fresh.IsStale);
        Assert.True(stale.IsStale);
        Assert.Equal(fresh.AsOfUtc, stale.AsOfUtc);
    }

    [Fact]
    public async Task بدون_داده_قبلی_قطعی_همچنان_خطا_می‌دهد()
    {
        var cache = new ProductCache(TimeProvider.System);
        await Assert.ThrowsAsync<PortalUnavailableException>(() => cache.GetDetailAsync(
            "101",
            _ => throw new PortalUnavailableException(),
            CancellationToken.None));
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now += duration;
    }
}
