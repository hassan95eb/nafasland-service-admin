using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Idempotency;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Catalog.Features.CreateProduct;

internal sealed record CreateProductCommand(
    string Title,
    string? Caption,
    string? Description,
    IReadOnlyList<PortalNameValue> Contents,
    bool CommentingEnabled,
    IReadOnlyList<PortalNameValue> Fields,
    string? Slug,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? MetaRobots,
    string? Redirect,
    IReadOnlyList<long> CategoryIds,
    IReadOnlyList<long> FilterIds,
    IReadOnlyList<PortalAttribute> Attributes,
    IReadOnlyList<CreateProductVariantInput> Variants)
    : ICommand<CreateProductResult>, IRequiresPermission, IIdempotentCommand, IAuditableCommand
{
    public string RequiredPermission => CatalogPermissions.ProductsWrite;
    public string AuditAction => "ProductCreated";
    public string AuditEntityType => "Product";
}

internal sealed record CreateProductVariantInput(
    string Title,
    decimal? Price,
    decimal? ComparePrice,
    decimal? Tax,
    decimal? Shipping,
    decimal? Weight,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    int? Stock,
    int? Minimum,
    int? Maximum,
    string? Sku);

internal sealed record CreateProductResult(string Id, string? Version, string? Title);
