using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Approvals;

/// <summary>
/// The same dev-environment test-product guard as UpdateProductCommandHandler
/// (ADR-029), applied to every one of this step's four writes. The step prompt
/// requires it checked twice: once when a request is filed (via PreviewAsync,
/// through <see cref="EnsureTestProduct"/> — throws, since a doomed request
/// should never be created at all) and again, unconditionally, right before the
/// real write in ExecuteAsync (via <see cref="CheckTestProduct"/> — returns a
/// Result instead of throwing, since ADR-010 rule 4 forbids letting execution
/// failures surface as unhandled exceptions), because the portal config could
/// have changed in between.
/// </summary>
internal static class CatalogApprovalGuard
{
    private const string Message = "محافظ محیط توسعه فقط اجازهٔ نوشتن روی محصول تستی تعیین‌شده را می‌دهد.";

    public static void EnsureTestProduct(string productId, PortalOptions options)
    {
        if (!IsTestProduct(productId, options))
        {
            throw new AuthorizationDeniedException(Message);
        }
    }

    public static Result? CheckTestProduct(string productId, PortalOptions options) =>
        IsTestProduct(productId, options) ? null : Result.Failure(Message);

    private static bool IsTestProduct(string productId, PortalOptions options) =>
        options.IsWriteAllowed(productId);
}
