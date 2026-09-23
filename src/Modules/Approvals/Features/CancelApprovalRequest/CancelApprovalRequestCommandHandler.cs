using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Approvals.Features.CancelApprovalRequest;

internal sealed class CancelApprovalRequestCommandHandler(
    ApprovalsDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    ApprovalExecutionCoordinator coordinator)
    : ICommandHandler<CancelApprovalRequestCommand, ApprovalDecisionResult>
{
    public async Task<ApprovalDecisionResult> HandleAsync(CancelApprovalRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await dbContext.ApprovalRequests.FindAsync([command.ApprovalRequestId], cancellationToken)
            ?? throw new ResourceNotFoundException("درخواست تأیید پیدا نشد.");

        var currentUserId = ApprovalActorInfo.RequireUserId(httpContextAccessor.HttpContext);
        if (request.RequestedByUserId != currentUserId)
        {
            throw new AuthorizationDeniedException("فقط کاربری که درخواست را ثبت کرده می‌تواند آن را لغو کند.");
        }

        request.EnsurePending();
        request.MarkCancelled(coordinator.UtcNow);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("این درخواست هم‌زمان تغییر کرد؛ صفحه را دوباره بارگذاری کنید.");
        }

        var (ipAddress, userAgent) = ApprovalActorInfo.RequestInfo(httpContextAccessor.HttpContext);
        await coordinator.WriteAuditAsync(
            request, "ApprovalCancelled", currentUserId, ApprovalActorInfo.RoleAtTime(httpContextAccessor.HttpContext), ipAddress, userAgent, cancellationToken);

        return new ApprovalDecisionResult(request.Id, request.Status.ToString(), request.ExecutionError);
    }
}
