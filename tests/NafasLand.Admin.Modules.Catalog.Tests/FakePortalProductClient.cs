using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;

namespace NafasLand.Admin.Modules.Catalog.Tests;

internal sealed class FakePortalProductClient : IPortalProductClient
{
    public int ListCallCount { get; private set; }
    public int DetailCallCount { get; private set; }
    public int VariantCallCount { get; private set; }
    public int UpdateVariantCallCount { get; private set; }
    public int CreateProductCallCount { get; private set; }
    public int UpdateProductCallCount { get; private set; }
    public TimeSpan Delay { get; init; }

    public PortalProductListResult ListResult { get; init; } = new(
        [new PortalProductSummary("101", "محصول نمونه", null, null, null, null, ["pending", "unavailable"])],
        1, 1, 1, 25);

    public PortalProductDetail? DetailResult { get; set; } = CreateDetail();

    public PortalProductVariant? VariantResult { get; set; } = new(
        "variant-1", "101", "primary", 100_000, null, null, null, null, null, null, null,
        4, null, null, "SKU-1", null, "commodity", [], []);

    public PortalVariantPatch? LastPatch { get; private set; }
    public PortalProductWriteModel? LastCreatedProduct { get; private set; }
    public PortalProductWriteModel? LastUpdatedProduct { get; private set; }

    public async Task<PortalProductListResult> ListProductsAsync(
        PortalProductListQuery query,
        CancellationToken cancellationToken)
    {
        ListCallCount++;
        await WaitAsync(cancellationToken);
        return ListResult with { Page = query.Page, PageSize = query.PageSize };
    }

    public async Task<PortalProductDetail?> GetProductAsync(string externalProductId, CancellationToken cancellationToken)
    {
        DetailCallCount++;
        await WaitAsync(cancellationToken);
        return DetailResult;
    }

    public async Task<PortalProductVariant?> GetVariantAsync(string externalVariantId, CancellationToken cancellationToken)
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

    public async Task<PortalProductCreateResult> CreateProductAsync(
        PortalProductWriteModel product,
        CancellationToken cancellationToken)
    {
        CreateProductCallCount++;
        LastCreatedProduct = product;
        await WaitAsync(cancellationToken);
        DetailResult = CreateDetail() with { Id = "202", Title = product.Title, Statuses = product.Status, Version = "2" };
        return new PortalProductCreateResult("202");
    }

    public async Task<PortalProductUpdateResult> UpdateProductAsync(
        string externalProductId,
        PortalProductWriteModel product,
        CancellationToken cancellationToken)
    {
        UpdateProductCallCount++;
        LastUpdatedProduct = product;
        await WaitAsync(cancellationToken);
        DetailResult = DetailResult is null ? null : DetailResult with
        {
            Title = product.Title,
            Description = product.Description,
            Contents = product.Contents,
            Version = "2",
        };
        return new PortalProductUpdateResult("2");
    }

    public Task<IReadOnlyList<PortalCategoryNode>> ListCategoriesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PortalCategoryNode>>([]);

    public Task<IReadOnlyList<PortalFilterGroup>> ListFiltersAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PortalFilterGroup>>([]);

    private Task WaitAsync(CancellationToken cancellationToken) =>
        Delay > TimeSpan.Zero ? Task.Delay(Delay, cancellationToken) : Task.CompletedTask;

    internal static PortalProductDetail CreateDetail() => new(
        "101",
        "محصول نمونه",
        null,
        null,
        null,
        null,
        [],
        false,
        [],
        [],
        null,
        null,
        null,
        ["pending", "unavailable"],
        [],
        [],
        null,
        [],
        [],
        [],
        null,
        null,
        null,
        null,
        null,
        null,
        "1");
}
