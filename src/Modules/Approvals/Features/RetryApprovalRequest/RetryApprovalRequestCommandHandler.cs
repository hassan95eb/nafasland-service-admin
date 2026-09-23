using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Approvals.Features.RetryApprovalRequest;

/// <summary>
/// Explicit, manual retry only (ADR-010, rule 4 forbids automatic retry on
/// delete — extended here to every execution, since the same "don't silently
/// repeat a risky portal write" reasoning applies to all four executors).
/// </summary>
internal sealed class RetryApprovalRequestCommandHandler(
    ApprovalsDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    ApprovalExecutionCoordinator coordinator)
    : ICommandHandler<RetryApprovalRequestCommand, ApprovalDecisionResult>
{
    public async Task<ApprovalDecisionResult> HandleAsync(RetryApprovalRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await dbContext.ApprovalRequests.FindAsync([command.ApprovalRequestId], cancellationToken)
            ?? throw new ResourceNotFoundException("درخواست تأیید پیدا نشد.");

        request.EnsureExecutionFailed();

        var reviewer = httpContextAccessor.HttpContext!.User;
        var reviewerUserId = ApprovalActorInfo.RequireUserId(httpContextAccessor.HttpContext);
        var executor = await coordinator.ResolveAndAuthorizeAsync(request, reviewer, cancellationToken);

        var (ipAddress, userAgent) = ApprovalActorInfo.RequestInfo(httpContextAccessor.HttpContext);
        var actorRoleAtTime = ApprovalActorInfo.RoleAtTime(httpContextAccessor.HttpContext);

        var executionResult = await coordinator.ExecuteSafelyAsync(executor, request, reviewerUserId, cancellationToken);

        if (executionResult.IsSuccess)
        {
            request.MarkExecuted(coordinator.UtcNow);
            await SaveWithConcurrencyCheckAsync(cancellationToken);
            await coordinator.WriteAuditAsync(request, "ApprovalExecuted", reviewerUserId, actorRoleAtTime, ipAddress, userAgent, cancellationToken);
        }
        else
        {
            request.MarkExecutionFailed(coordinator.UtcNow, executionResult.Error!);
            await SaveWithConcurrencyCheckAsync(cancellationToken);
            await coordinator.WriteAuditAsync(
                request, "ApprovalExecutionFailed", reviewerUserId, actorRoleAtTime, ipAddress, userAgent, cancellationToken, executionResult.Error);
        }

        return new ApprovalDecisionResult(request.Id, request.Status.ToString(), request.ExecutionError);
    }

    private async Task SaveWithConcurrencyCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("این درخواست هم‌زمان توسط کاربر دیگری تغییر کرد؛ صفحه را دوباره بارگذاری کنید.");
        }
    }
}
