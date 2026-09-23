using Microsoft.AspNetCore.Http;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Modules.Approvals.Jobs;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Approvals.Features.CreateApprovalRequest;

internal sealed class CreateApprovalRequestCommandHandler(
    ApprovalsDbContext dbContext,
    IApprovalExecutorRegistry registry,
    IHttpContextAccessor httpContextAccessor,
    ApprovalExecutionCoordinator coordinator)
    : ICommandHandler<CreateApprovalRequestCommand, CreateApprovalRequestResult>
{
    public async Task<CreateApprovalRequestResult> HandleAsync(CreateApprovalRequestCommand command, CancellationToken cancellationToken)
    {
        var executor = registry.Resolve(command.RequestType)
            ?? throw new BusinessRuleException("نوع درخواست تأیید نامعتبر است.");

        var requestedByUserId = ApprovalActorInfo.RequireUserId(httpContextAccessor.HttpContext);
        var now = coordinator.UtcNow;

        // Best-effort — a live preview is a display convenience for the list view,
        // not something the request's validity depends on; the target entity is
        // always re-read live again at review and execution time regardless
        // (ADR-010, rule 5).
        string? snapshotJson = null;
        try
        {
            var preview = await executor.PreviewAsync(command.PayloadJson, cancellationToken);
            snapshotJson = System.Text.Json.JsonSerializer.Serialize(preview);
        }
        catch (Exception exception) when (exception is ResourceNotFoundException or PortalUnavailableException)
        {
            // Left null; the review/detail page will surface the live error itself.
        }

        var request = ApprovalRequest.Create(
            command.RequestType,
            command.TargetEntityType,
            command.TargetEntityId,
            command.PayloadJson,
            snapshotJson,
            command.Reason,
            requestedByUserId,
            now,
            now + ApprovalExpiryPolicy.ExpiryPeriod);

        dbContext.ApprovalRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);

        var (ipAddress, userAgent) = ApprovalActorInfo.RequestInfo(httpContextAccessor.HttpContext);
        await coordinator.WriteAuditAsync(
            request,
            "ApprovalRequested",
            requestedByUserId,
            ApprovalActorInfo.RoleAtTime(httpContextAccessor.HttpContext),
            ipAddress,
            userAgent,
            cancellationToken);

        return new CreateApprovalRequestResult(request.Id, request.RequestType, request.Status.ToString(), request.RequestedAt, request.ExpiresAt);
    }
}
