using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;

namespace NafasLand.Admin.Modules.Catalog.Tests;

internal sealed class FakePortalProductClient : IPortalProductClient
{
    public int ListCallCount { get; private set; }

    public int DetailCallCount { get; private set; }

    public TimeSpan Delay { get; init; }

    public PortalProductListResult ListResult { get; init; } = new(
        [new PortalProductSummary("101", "محصول نمونه", null, null, null, null, ["pending", "unavailable"])],
        1,
        1,
        1,
        25);

    public PortalProductDetail? DetailResult { get; set; } = CreateDetail();

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
