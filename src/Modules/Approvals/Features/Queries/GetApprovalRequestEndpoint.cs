using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Approvals.Features.Queries;

internal static class GetApprovalRequestEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/approvals/{id:guid}", async (
                Guid id,
                HttpContext httpContext,
                ApprovalsDbContext dbContext,
                IApprovalExecutorRegistry registry,
                CancellationToken cancellationToken) =>
            {
                var request = await dbContext.ApprovalRequests.AsNoTracking()
                    .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
                    ?? throw new ResourceNotFoundException("درخواست تأیید پیدا نشد.");

                var currentUserId = ApprovalActorInfo.RequireUserId(httpContext);
                var canReadAll = httpContext.User.HasClaim(PermissionClaimTypes.Permission, ApprovalsPermissions.ReadAll);
                if (!canReadAll && request.RequestedByUserId != currentUserId)
                {
                    throw new AuthorizationDeniedException("این درخواست متعلق به شما نیست.");
                }

                // Live preview only while the decision is still actionable
                // (ADR-010, rule 5) — once terminal, there is nothing left to
                // preview against, and the target may no longer even exist.
                ApprovalPreview? preview = null;
                if (request.Status is ApprovalRequestStatus.Pending or ApprovalRequestStatus.ExecutionFailed)
                {
                    var executor = registry.Resolve(request.RequestType);
                    if (executor is not null)
                    {
                        try
                        {
                            preview = await executor.PreviewAsync(request.PayloadJson, cancellationToken);
                        }
                        catch (Exception exception) when (
                            exception is ResourceNotFoundException or PortalUnavailableException or AuthorizationDeniedException)
                        {
                            // Left null; the client falls back to the stored SnapshotJson/reason.
                            // AuthorizationDeniedException here means the executor's own
                            // test-product guard (ADR-029) rejected the live preview —
                            // e.g. PortalOptions.TestProductId changed after this request
                            // was filed. The request itself must still be viewable (and
                            // rejectable/cancellable) even though it can no longer be
                            // approved; ExecuteAsync enforces the real block at decision time.
                        }
                    }
                }

                return Results.Ok(ApprovalRequestDetailDto.FromEntity(request, preview));
            })
            .RequireAuthorization();
    }
}
