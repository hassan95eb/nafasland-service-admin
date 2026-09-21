using System.Collections.Concurrent;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Infrastructure;

internal sealed class ProductCache(TimeProvider timeProvider)
{
    private static readonly TimeSpan FreshDuration = TimeSpan.FromSeconds(60);
    private readonly ConcurrentDictionary<ProductListCacheKey, CacheEntry<PortalProductListResult>> _lists = new();
    private readonly ConcurrentDictionary<string, CacheEntry<PortalProductDetail?>> _details = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<object, SemaphoreSlim> _locks = new();

    public Task<PortalProductListResult> GetListAsync(
        PortalProductListQuery query,
        Func<CancellationToken, Task<PortalProductListResult>> factory,
        CancellationToken cancellationToken)
    {
        var key = new ProductListCacheKey(query.Page, query.PageSize, query.Keywords, query.Sorting);
        return GetOrRefreshAsync(
            key,
            _lists,
            factory,
            (value, isStale, asOf) => value with { IsStale = isStale, AsOfUtc = asOf },
            cancellationToken);
    }

    public Task<PortalProductDetail?> GetDetailAsync(
        string externalProductId,
        Func<CancellationToken, Task<PortalProductDetail?>> factory,
        CancellationToken cancellationToken)
    {
        return GetOrRefreshAsync(
            externalProductId,
            _details,
            factory,
            (value, isStale, asOf) => value is null ? null : value with { IsStale = isStale, AsOfUtc = asOf },
            cancellationToken);
    }

    private async Task<TValue> GetOrRefreshAsync<TKey, TValue>(
        TKey key,
        ConcurrentDictionary<TKey, CacheEntry<TValue>> entries,
        Func<CancellationToken, Task<TValue>> factory,
        Func<TValue, bool, DateTimeOffset, TValue> mark,
        CancellationToken cancellationToken)
        where TKey : notnull
    {
        var now = timeProvider.GetUtcNow();
        if (entries.TryGetValue(key, out var current) && now - current.AsOfUtc <= FreshDuration)
        {
            return mark(current.Value, false, current.AsOfUtc);
        }

        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            now = timeProvider.GetUtcNow();
            if (entries.TryGetValue(key, out current) && now - current.AsOfUtc <= FreshDuration)
            {
                return mark(current.Value, false, current.AsOfUtc);
            }

            try
            {
                var value = await factory(cancellationToken);
                var stamped = mark(value, false, now);
                entries[key] = new CacheEntry<TValue>(stamped, now);
                return stamped;
            }
            catch (PortalUnavailableException) when (entries.TryGetValue(key, out current))
            {
                return mark(current.Value, true, current.AsOfUtc);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed record ProductListCacheKey(int Page, int PageSize, string? Keywords, string? Sorting);

    private sealed record CacheEntry<T>(T Value, DateTimeOffset AsOfUtc);
}
