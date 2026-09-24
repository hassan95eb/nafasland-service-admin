using System.Text.Json;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Approvals;

/// <summary>catalog.product.status — ADR-031. One request toggles exactly one of "featured"/"most", never both at once.</summary>
internal sealed class ProductStatusApprovalExecutor(IPortalProductClient portalClient, IOptions<PortalOptions> portalOptions) : IApprovalExecutor
{
    public const string RequestTypeKey = "catalog.product.status";

    private static readonly string[] AllowedStatusKeys = ["featured", "most"];

    public string RequestType => RequestTypeKey;

    public string RequestPermission => CatalogPermissions.ProductsStatusRequest;

    public string RequiredPermission => CatalogPermissions.ProductsStatus;

    public async Task<ApprovalPreview> PreviewAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = Deserialize(payloadJson);
        CatalogApprovalGuard.EnsureTestProduct(payload.ProductId, portalOptions.Value);

        var product = await portalClient.GetProductAsync(payload.ProductId, cancellationToken)
            ?? throw new ResourceNotFoundException("محصول در نفس‌لند پیدا نشد.");

        var isCurrentlySet = product.Statuses.Contains(payload.StatusKey, StringComparer.OrdinalIgnoreCase);
        return new ApprovalPreview(product.Title ?? payload.ProductId,
        [
            new ApprovalPreviewField("وضعیت هدف", DisplayName(payload.StatusKey)),
            new ApprovalPreviewField("جهت درخواست", isCurrentlySet ? "خاموش‌کردن" : "روشن‌کردن"),
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
                ?? throw new ResourceNotFoundException("محصول در نفس‌لند پیدا نشد.");

            var status = new List<string>(current.Statuses);
            var wasSet = status.RemoveAll(value => string.Equals(value, payload.StatusKey, StringComparison.OrdinalIgnoreCase)) > 0;
            if (!wasSet)
            {
                status.Add(payload.StatusKey);
            }

            var merged = PortalProductMapper.ToWriteModel(current) with { Status = status };
            await portalClient.UpdateProductAsync(payload.ProductId, merged, cancellationToken);
            return Result.Success();
        }
        catch (Exception exception) when (exception is PortalUnavailableException or PortalBusyException or ResourceNotFoundException)
        {
            return Result.Failure(exception.Message);
        }
    }

    private static string DisplayName(string statusKey) => statusKey switch
    {
        "featured" => "ویژه (featured)",
        "most" => "پرفروش‌ترین (most)",
        _ => statusKey,
    };

    private static ProductStatusPayload Deserialize(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<ProductStatusPayload>(payloadJson, CatalogApprovalJson.Options)
            ?? throw new BusinessRuleException("بدنهٔ درخواست تغییر وضعیت محصول نامعتبر است.");

        if (!AllowedStatusKeys.Contains(payload.StatusKey, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("وضعیت درخواستی فقط می‌تواند featured یا most باشد.");
        }

        return payload;
    }
}

internal sealed record ProductStatusPayload(string ProductId, string StatusKey);
