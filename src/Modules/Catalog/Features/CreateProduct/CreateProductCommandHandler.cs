using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Catalog.Features.CreateProduct;

internal sealed class CreateProductCommandHandler(
    IPortalProductClient portalClient,
    IOptions<PortalOptions> portalOptions,
    IProductHtmlSanitizer htmlSanitizer,
    ProductCache cache,
    IAuditContext auditContext,
    IProductRefWriter productRefWriter)
    : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        if (!portalOptions.Value.AllowProductCreation)
        {
            throw new BusinessRuleException(
                "ایجاد محصول در این محیط غیرفعال است. گزینهٔ Portal:AllowProductCreation باید صریحاً فعال شود.");
        }

        var writeModel = new PortalProductWriteModel(
            command.Title,
            command.Caption,
            htmlSanitizer.Sanitize(command.Description),
            command.Contents.Select(item => item with { Value = htmlSanitizer.Sanitize(item.Value) }).ToArray(),
            command.CommentingEnabled,
            command.Fields,
            command.Slug,
            command.MetaTitle,
            command.MetaDescription,
            command.MetaKeywords,
            command.MetaRobots,
            command.Redirect,
            CanonicalUrl: null,
            Image: null,
            Images: null,
            command.CategoryIds,
            command.FilterIds,
            Relates: [],
            command.Attributes,
            command.Variants.Select(MapVariant).ToArray(),
            Status: ["pending"],
            Published: null);

        var created = await portalClient.CreateProductAsync(writeModel, cancellationToken);
        auditContext.SetEntityId(created.Id);

        var actual = await portalClient.GetProductAsync(created.Id, cancellationToken)
            ?? throw new PortalUnavailableException();
        auditContext.SetAfter(actual);
        cache.InvalidateLists();

        // ADR-009's ProductRef, from what the portal actually stored.
        if (!string.IsNullOrWhiteSpace(actual.Title))
        {
            await productRefWriter.UpsertAsync(actual.Id, actual.Title, cancellationToken);
        }

        return new CreateProductResult(actual.Id, actual.Version, actual.Title);
    }

    private static PortalProductVariant MapVariant(CreateProductVariantInput variant) => new(
        Id: null,
        ProductId: null,
        variant.Title,
        variant.Price,
        variant.ComparePrice,
        variant.Tax,
        variant.Shipping,
        variant.Weight,
        variant.Length,
        variant.Width,
        variant.Height,
        variant.Stock,
        variant.Minimum,
        variant.Maximum,
        variant.Sku,
        Image: null,
        Type: "commodity",
        Status: [],
        Files: []);
}
