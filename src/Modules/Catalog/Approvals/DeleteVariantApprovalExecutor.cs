using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Approvals;

/// <summary>catalog.variant.delete — ADR-032. Refuses to remove a product's last variant, which would leave it priceless and unpurchasable.</summary>
internal sealed class DeleteVariantApprovalExecutor(IPortalProductClient portalClient, IOptions<PortalOptions> portalOptions) : IApprovalExecutor
{
    public const string RequestTypeKey = "catalog.variant.delete";

    public string RequestType => RequestTypeKey;

    public string RequestPermission => CatalogPermissions.VariantsDeleteRequest;

    public string RequiredPermission => CatalogPermissions.VariantsDelete;

    public async Task<ApprovalPreview> PreviewAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = Deserialize(payloadJson);
        CatalogApprovalGuard.EnsureTestProduct(payload.ProductId, portalOptions.Value);

        var product = await portalClient.GetProductAsync(payload.ProductId, cancellationToken)
            ?? throw new ResourceNotFoundException("محصول در نفس‌لند پیدا نشد.");
        var variant = product.Variants.FirstOrDefault(candidate => candidate.Id == payload.VariantId);

        return new ApprovalPreview(product.Title ?? payload.ProductId,
        [
            new ApprovalPreviewField("واریانت هدف", variant?.Sku ?? variant?.Title ?? payload.VariantId),
            new ApprovalPreviewField("قیمت واریانت", variant?.Price?.ToString(CultureInfo.InvariantCulture)),
            new ApprovalPreviewField("موجودی واریانت", variant?.Stock?.ToString(CultureInfo.InvariantCulture)),
            new ApprovalPreviewField("تعداد کل واریانت‌های محصول", product.Variants.Count.ToString(CultureInfo.InvariantCulture)),
        ]);
    }

    public async Task<Result> ExecuteAsync(string payloadJson, ApprovalContext context, CancellationToken cancellationToken)
    {
        var payload = Deserialize(payloadJson);
        var guardFailure = CatalogApprovalGuard.CheckTestProduct(payload.ProductId, portalOptions.Value);
        if (guardFailure is not null)
        {
            return guardFailure;
        }

        try
        {
            var current = await portalClient.GetProductAsync(payload.ProductId, cancellationToken)
                ?? throw new ResourceNotFoundException("محصول در نفس‌لند پیدا نشد.");

            if (current.Variants.Count <= 1)
            {
                return Result.Failure("این محصول فقط یک واریانت دارد؛ حذف آن محصول را بی‌قیمت و غیرقابل‌خرید می‌کند (ADR-032).");
            }

            var remaining = current.Variants.Where(variant => variant.Id != payload.VariantId).ToArray();
            if (remaining.Length == current.Variants.Count)
            {
                return Result.Failure("واریانت هدف در محصول پیدا نشد؛ ممکن است پیش‌تر حذف شده باشد.");
            }

            var merged = PortalProductMapper.ToWriteModel(current) with { Variants = remaining };
            await portalClient.UpdateProductAsync(payload.ProductId, merged, cancellationToken);
            return Result.Success();
        }
        catch (Exception exception) when (exception is PortalUnavailableException or PortalBusyException or ResourceNotFoundException)
        {
            return Result.Failure(exception.Message);
        }
    }

    private static DeleteVariantPayload Deserialize(string payloadJson) =>
        JsonSerializer.Deserialize<DeleteVariantPayload>(payloadJson, CatalogApprovalJson.Options)
        ?? throw new BusinessRuleException("بدنهٔ درخواست حذف واریانت نامعتبر است.");
}

internal sealed record DeleteVariantPayload(string ProductId, string VariantId);
