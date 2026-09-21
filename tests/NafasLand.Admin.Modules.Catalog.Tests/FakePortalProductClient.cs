using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;

namespace NafasLand.Admin.Modules.Catalog.Tests;

internal sealed class FakePortalProductClient : IPortalProductClient
{
    public int ListCallCount { get; private set; }

    public int DetailCallCount { get; private set; }

    public int VariantCallCount { get; private set; }

    public int UpdateVariantCallCount { get; private set; }

    public TimeSpan Delay { get; init; }

    public PortalProductListResult ListResult { get; init; } = new(
        [new PortalProductSummary("101", "محصول نمونه", null, null, null, null, ["pending", "unavailable"])],
        1,
        1,
        1,
        25);

    public PortalProductDetail? DetailResult { get; set; } = CreateDetail();

    public PortalProductVariant? VariantResult { get; set; } = new(
        "variant-1", "101", "SKU-1", 100_000, null, 4, null, null);

    public PortalVariantPatch? LastPatch { get; private set; }

    public async Task<PortalProductListResult> ListProductsAsync(
        PortalProductListQuery query,
        CancellationToken cancellationToken)
    {
        ListCallCount++;
        await WaitAsync(cancellationToken);
        return ListResult with { Page = query.Page, PageSize = query.PageSize };
    }

    public async Task<PortalProductDetail?> GetProductAsync(
        string externalProductId,
        CancellationToken cancellationToken)
    {
        DetailCallCount++;
        await WaitAsync(cancellationToken);
        return DetailResult;
    }

    public async Task<PortalProductVariant?> GetVariantAsync(
        string externalVariantId,
        CancellationToken cancellationToken)
    {
        VariantCallCount++;
        await WaitAsync(cancellationToken);
        return VariantResult;
    }

    public async Task UpdateVariantAsync(
        string externalVariantId,
        PortalVariantPatch patch,
        CancellationToken cancellationToken)
    {
        UpdateVariantCallCount++;
        LastPatch = patch;
        await WaitAsync(cancellationToken);
        if (VariantResult is not null)
        {
            VariantResult = VariantResult with
            {
                Price = patch.Price ?? VariantResult.Price,
                Stock = patch.Stock ?? VariantResult.Stock,
            };
        }
    }

    private Task WaitAsync(CancellationToken cancellationToken)
    {
        return Delay > TimeSpan.Zero ? Task.Delay(Delay, cancellationToken) : Task.CompletedTask;
    }

    private static PortalProductDetail CreateDetail()
    {
        return new PortalProductDetail(
            "101",
            "محصول نمونه",
            null,
            null,
            null,
            null,
            null,
            null,
            ["pending", "unavailable"],
            [],
            [],
            [],
            [],
            [],
            [],
            null,
            null);
    }
}
