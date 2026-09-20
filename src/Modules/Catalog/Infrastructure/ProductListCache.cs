using Microsoft.Extensions.Caching.Memory;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;

namespace NafasLand.Admin.Modules.Catalog.Infrastructure;

internal sealed class ProductListCache(IMemoryCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public Task<PortalProductListResult> GetOrCreateAsync(
        PortalProductListQuery query,
        Func<CancellationToken, Task<PortalProductListResult>> factory,
        CancellationToken cancellationToken)
    {
        var key = new ProductListCacheKey(query.Page, query.PageSize, query.Keywords, query.Sorting);
        return cache.GetOrCreateAsync(
            key,
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return factory(cancellationToken);
            })!;
    }

    private sealed record ProductListCacheKey(int Page, int PageSize, string? Keywords, string? Sorting);
}
