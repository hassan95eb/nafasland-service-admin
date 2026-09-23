using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Approvals;

/// <summary>
/// catalog.product.publish — ADR-030. Toggles pending⇄approved; which direction
/// is decided by the request's own payload, not by two separate RequestTypes,
/// since both directions share the exact same read-merge-write shape.
/// </summary>
internal sealed class PublishProductApprovalExecutor(IPortalProductClient portalClient, IOptions<PortalOptions> portalOptions) : IApprovalExecutor
{
    public const string RequestTypeKey = "catalog.product.publish";

    public string RequestType => RequestTypeKey;

    public string RequestPermission => CatalogPermissions.ProductsPublishRequest;

    public string RequiredPermission => CatalogPermissions.ProductsPublish;

    public async Task<ApprovalPreview> PreviewAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = Deserialize(payloadJson);
        CatalogApprovalGuard.EnsureTestProduct(payload.ProductId, portalOptions.Value);

        var product = await portalClient.GetProductAsync(payload.ProductId, cancellationToken)
            ?? throw new ResourceNotFoundException("محصول در پرتال پیدا نشد.");

        return new ApprovalPreview(product.Title ?? payload.ProductId,
        [
            new ApprovalPreviewField("جهت درخواست", payload.Publish ? "انتشار (pending ← approved)" : "لغو انتشار (approved ← pending)"),
            new ApprovalPreviewField("قیمت", product.Price?.ToString(CultureInfo.InvariantCulture)),
            new ApprovalPreviewField("موجودی مجموع واریانت‌ها", product.Variants.Sum(variant => variant.Stock ?? 0).ToString(CultureInfo.InvariantCulture)),
            new ApprovalPreviewField("تصاویر", product.Images.Count.ToString(CultureInfo.InvariantCulture)),
            new ApprovalPreviewField("دسته‌بندی‌ها", string.Join("، ", product.Categories.Select(category => category.Title ?? category.Id))),
            new ApprovalPreviewField("وضعیت فعلی", string.Join("، ", product.Statuses)),
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
                ?? throw new ResourceNotFoundException("محصول در پرتال پیدا نشد.");

            var status = new List<string>(current.Statuses);
            if (payload.Publish)
            {
                status.RemoveAll(value => string.Equals(value, "pending", StringComparison.OrdinalIgnoreCase));
                if (!status.Contains("approved", StringComparer.OrdinalIgnoreCase))
                {
                    status.Add("approved");
                }
            }
            else
            {
                status.RemoveAll(value => string.Equals(value, "approved", StringComparison.OrdinalIgnoreCase));
                if (!status.Contains("pending", StringComparer.OrdinalIgnoreCase))
                {
                    status.Add("pending");
                }
            }

            // Read-merge-write, the exact pattern UpdateProductCommandHandler uses
            // (ADR-017): only Status changes, every other field is preserved as-is.
            var merged = PortalProductMapper.ToWriteModel(current) with { Status = status };
            await portalClient.UpdateProductAsync(payload.ProductId, merged, cancellationToken);
            return Result.Success();
        }
        catch (Exception exception) when (exception is PortalUnavailableException or PortalBusyException or ResourceNotFoundException)
        {
            return Result.Failure(exception.Message);
        }
    }

    private static PublishProductPayload Deserialize(string payloadJson) =>
        JsonSerializer.Deserialize<PublishProductPayload>(payloadJson, CatalogApprovalJson.Options)
        ?? throw new BusinessRuleException("بدنهٔ درخواست انتشار محصول نامعتبر است.");
}

internal sealed record PublishProductPayload(string ProductId, bool Publish);
