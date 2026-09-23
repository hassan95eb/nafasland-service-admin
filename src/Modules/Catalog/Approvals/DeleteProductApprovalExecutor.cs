using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Approvals;

/// <summary>catalog.product.delete — ADR-010/ADR-002. No automatic retry (ADR-010, rule 4): a failed delete lands on ExecutionFailed and waits for an explicit retry.</summary>
internal sealed class DeleteProductApprovalExecutor(IPortalProductClient portalClient, IOptions<PortalOptions> portalOptions) : IApprovalExecutor
{
    public const string RequestTypeKey = "catalog.product.delete";

    public string RequestType => RequestTypeKey;

    public string RequestPermission => CatalogPermissions.ProductsDeleteRequest;

    public string RequiredPermission => CatalogPermissions.ProductsDelete;

    public async Task<ApprovalPreview> PreviewAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = Deserialize(payloadJson);
        CatalogApprovalGuard.EnsureTestProduct(payload.ProductId, portalOptions.Value);

        var product = await portalClient.GetProductAsync(payload.ProductId, cancellationToken)
            ?? throw new ResourceNotFoundException("محصول در پرتال پیدا نشد.");

        return new ApprovalPreview(product.Title ?? payload.ProductId,
        [
            new ApprovalPreviewField("قیمت", product.Price?.ToString(CultureInfo.InvariantCulture)),
            new ApprovalPreviewField("تعداد واریانت", product.Variants.Count.ToString(CultureInfo.InvariantCulture)),
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
            await portalClient.DeleteProductAsync(payload.ProductId, cancellationToken);
            return Result.Success();
        }
        catch (Exception exception) when (exception is PortalUnavailableException or PortalBusyException)
        {
            return Result.Failure(exception.Message);
        }
    }

    private static DeleteProductPayload Deserialize(string payloadJson) =>
        JsonSerializer.Deserialize<DeleteProductPayload>(payloadJson, CatalogApprovalJson.Options)
        ?? throw new BusinessRuleException("بدنهٔ درخواست حذف محصول نامعتبر است.");
}

internal sealed record DeleteProductPayload(string ProductId);
