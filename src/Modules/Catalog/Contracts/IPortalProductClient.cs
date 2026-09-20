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
}
