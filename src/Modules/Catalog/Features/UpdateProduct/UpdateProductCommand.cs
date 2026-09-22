using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Catalog.Features.UpdateProduct;

internal sealed record UpdateProductCommand(
    string ProductId,
    string LastKnownVersion,
    string Title,
    string? Caption,
    string? Description,
    bool DescriptionDirty,
    IReadOnlyList<UpdateProductContentInput> Contents,
    bool CommentingEnabled,
    IReadOnlyList<PortalNameValue> Fields,
    string? Slug,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? MetaRobots,
    string? Redirect,
    IReadOnlyList<long> CategoryIds,
    IReadOnlyList<long> FilterIds)
    : ICommand<UpdateProductResult>, IRequiresPermission, IAuditableCommand
{
    public string RequiredPermission => CatalogPermissions.ProductsWrite;
    public string AuditAction => "ProductUpdated";
    public string AuditEntityType => "Product";
}

internal sealed record UpdateProductContentInput(string Name, string? Value, bool IsDirty);
internal sealed record UpdateProductResult(string Id, string? Version, string? Title);
