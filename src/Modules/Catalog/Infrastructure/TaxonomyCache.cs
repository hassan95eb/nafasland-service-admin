using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Infrastructure;

internal sealed class TaxonomyCache(TimeProvider timeProvider)
{
    private static readonly TimeSpan FreshDuration = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _categoriesLock = new(1, 1);
    private readonly SemaphoreSlim _filtersLock = new(1, 1);
    private Entry<PortalCategoryNode>? _categories;
    private Entry<PortalFilterGroup>? _filters;

    public Task<PortalTaxonomyResult<PortalCategoryNode>> GetCategoriesAsync(
        Func<CancellationToken, Task<IReadOnlyList<PortalCategoryNode>>> factory,
        CancellationToken cancellationToken) =>
        GetAsync(_categoriesLock, () => _categories, value => _categories = value, factory, cancellationToken);

    public Task<PortalTaxonomyResult<PortalFilterGroup>> GetFiltersAsync(
        Func<CancellationToken, Task<IReadOnlyList<PortalFilterGroup>>> factory,
        CancellationToken cancellationToken) =>
        GetAsync(_filtersLock, () => _filters, value => _filters = value, factory, cancellationToken);

    private async Task<PortalTaxonomyResult<T>> GetAsync<T>(
        SemaphoreSlim gate,
        Func<Entry<T>?> read,
        Action<Entry<T>> write,
        Func<CancellationToken, Task<IReadOnlyList<T>>> factory,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var current = read();
        if (current is not null && now - current.AsOfUtc <= FreshDuration)
        {
            return new PortalTaxonomyResult<T>(current.Items, false, current.AsOfUtc);
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            now = timeProvider.GetUtcNow();
            current = read();
            if (current is not null && now - current.AsOfUtc <= FreshDuration)
            {
                return new PortalTaxonomyResult<T>(current.Items, false, current.AsOfUtc);
            }

            try
            {
                var items = await factory(cancellationToken);
                write(new Entry<T>(items, now));
                return new PortalTaxonomyResult<T>(items, false, now);
            }
            catch (PortalUnavailableException) when (current is not null)
            {
                return new PortalTaxonomyResult<T>(current.Items, true, current.AsOfUtc);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed record Entry<T>(IReadOnlyList<T> Items, DateTimeOffset AsOfUtc);
}
