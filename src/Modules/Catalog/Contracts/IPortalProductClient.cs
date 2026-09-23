using NafasLand.Admin.Modules.Catalog.Contracts.Models;

namespace NafasLand.Admin.Modules.Catalog.Contracts;

public interface IPortalProductClient
{
    public Task<PortalProductListResult> ListProductsAsync(
        PortalProductListQuery query,
        CancellationToken cancellationToken);

    public Task<PortalProductDetail?> GetProductAsync(
        string externalProductId,
        CancellationToken cancellationToken);

    public Task<PortalProductVariant?> GetVariantAsync(
        string externalVariantId,
        CancellationToken cancellationToken);

    public Task UpdateVariantAsync(
        string externalVariantId,
        PortalVariantPatch patch,
        CancellationToken cancellationToken);

    Task<PortalProductCreateResult> CreateProductAsync(
        PortalProductWriteModel product,
        CancellationToken cancellationToken);

    Task<PortalProductUpdateResult> UpdateProductAsync(
        string externalProductId,
        PortalProductWriteModel product,
        CancellationToken cancellationToken);

    /// <summary>DELETE /manage/store/products/:id (ADR-017) — used only by the catalog.product.delete approval executor, never called directly from an Admin-facing command.</summary>
    Task DeleteProductAsync(string externalProductId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PortalCategoryNode>> ListCategoriesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PortalFilterGroup>> ListFiltersAsync(CancellationToken cancellationToken);
}
