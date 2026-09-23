using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Approvals.Features.RejectApprovalRequest;

/// <summary>Never calls the portal (acceptance criterion 3): rejecting is purely a status transition plus an audit row.</summary>
internal sealed class RejectApprovalRequestCommandHandler(
    ApprovalsDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    ApprovalExecutionCoordinator coordinator)
    : ICommandHandler<RejectApprovalRequestCommand, ApprovalDecisionResult>
{
    public async Task<ApprovalDecisionResult> HandleAsync(RejectApprovalRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await dbContext.ApprovalRequests.FindAsync([command.ApprovalRequestId], cancellationToken)
            ?? throw new ResourceNotFoundException("درخواست تأیید پیدا نشد.");

        request.EnsurePending();

        var reviewerUserId = ApprovalActorInfo.RequireUserId(httpContextAccessor.HttpContext);
        request.MarkRejected(reviewerUserId, coordinator.UtcNow, command.Note);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("این درخواست هم‌زمان توسط کاربر دیگری تغییر کرد؛ صفحه را دوباره بارگذاری کنید.");
        }

        var (ipAddress, userAgent) = ApprovalActorInfo.RequestInfo(httpContextAccessor.HttpContext);
        await coordinator.WriteAuditAsync(
            request, "ApprovalRejected", reviewerUserId, ApprovalActorInfo.RoleAtTime(httpContextAccessor.HttpContext), ipAddress, userAgent, cancellationToken);

        return new ApprovalDecisionResult(request.Id, request.Status.ToString(), request.ExecutionError);
    }
}
