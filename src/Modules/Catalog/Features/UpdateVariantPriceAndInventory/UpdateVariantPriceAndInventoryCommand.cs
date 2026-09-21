using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Idempotency;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Catalog.Features.UpdateVariantPriceAndInventory;

internal sealed record UpdateVariantPriceAndInventoryCommand(
    string VariantId,
    decimal NewPrice,
    int NewStock,
    decimal? LastKnownPrice,
    int? LastKnownStock)
    : ICommand<UpdateVariantPriceAndInventoryResult>, IRequiresPermission, IIdempotentCommand, IAuditableCommand
{
    public string RequiredPermission => CatalogPermissions.ProductsWrite;

    public string AuditAction => "VariantPriceInventoryUpdated";

    public string AuditEntityType => "ProductVariant";
}

internal sealed record UpdateVariantPriceAndInventoryResult(string VariantId, decimal Price, int Stock);
