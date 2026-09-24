using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Catalog.Features.UpdateProduct;

internal sealed class UpdateProductCommandHandler(
    IPortalProductClient portalClient,
    IOptions<PortalOptions> portalOptions,
    IProductHtmlSanitizer htmlSanitizer,
    ProductCache cache,
    IAuditContext auditContext,
    IProductRefWriter productRefWriter)
    : ICommandHandler<UpdateProductCommand, UpdateProductResult>
{
    public async Task<UpdateProductResult> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        auditContext.SetEntityId(command.ProductId);
        var current = await portalClient.GetProductAsync(command.ProductId, cancellationToken)
            ?? throw new ResourceNotFoundException("محصول در نفس‌لند پیدا نشد.");

        auditContext.SetBefore(current);

        if (!portalOptions.Value.IsWriteAllowed(current.Id))
        {
            throw new AuthorizationDeniedException(
                "محافظ محیط توسعه فقط اجازهٔ نوشتن روی محصول تستی تعیین‌شده را می‌دهد.");
        }

        if (!string.Equals(current.Version, command.LastKnownVersion, StringComparison.Ordinal))
        {
            throw new ConflictException("محصول از زمان بازشدن فرم تغییر کرده است؛ صفحه را دوباره بارگذاری کنید.");
        }

        if (current.Statuses.Count == 0)
        {
            throw new BusinessRuleException(
                "نفس‌لند آرایهٔ کامل وضعیت محصول را برنگرداند؛ برای جلوگیری از تغییر ناخواسته، ذخیره انجام نشد.");
        }

        var preserved = PortalProductMapper.ToWriteModel(current);
        var merged = preserved with
        {
            Title = command.Title,
            Caption = command.Caption,
            Description = command.DescriptionDirty
                ? htmlSanitizer.Sanitize(command.Description)
                : current.Description,
            Contents = MergeContents(current.Contents, command.Contents),
            CommentingEnabled = command.CommentingEnabled,
            Fields = command.Fields,
            Slug = command.Slug,
            MetaTitle = command.MetaTitle,
            MetaDescription = command.MetaDescription,
            MetaKeywords = command.MetaKeywords,
            MetaRobots = command.MetaRobots,
            Redirect = command.Redirect,
            Categories = command.CategoryIds,
            Filters = command.FilterIds,
        };

        await portalClient.UpdateProductAsync(command.ProductId, merged, cancellationToken);
        var actual = await portalClient.GetProductAsync(command.ProductId, cancellationToken)
            ?? throw new PortalUnavailableException();

        auditContext.SetAfter(actual);
        cache.InvalidateProduct(command.ProductId);

        // ADR-009's ProductRef, from what the portal actually stored.
        if (!string.IsNullOrWhiteSpace(actual.Title))
        {
            await productRefWriter.UpsertAsync(actual.Id, actual.Title, cancellationToken);
        }

        return new UpdateProductResult(actual.Id, actual.Version, actual.Title);
    }

    private IReadOnlyList<PortalNameValue> MergeContents(
        IReadOnlyList<PortalNameValue> current,
        IReadOnlyList<UpdateProductContentInput> requested)
    {
        var originals = current.ToDictionary(value => value.Name, StringComparer.Ordinal);
        return requested.Select(item =>
        {
            if (!item.IsDirty && originals.TryGetValue(item.Name, out var original))
            {
                return original;
            }

            return new PortalNameValue(item.Name, htmlSanitizer.Sanitize(item.Value));
        }).ToArray();
    }
}
