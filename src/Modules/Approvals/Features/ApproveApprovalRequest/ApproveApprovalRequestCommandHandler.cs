using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;

/// <summary>
/// Approval and execution happen in this one command/one HTTP request (an open
/// decision the step prompt left to us, reported in the PR description): the
/// alternative — a separate background execution step — would need its own job
/// queue and a second "executing" state with no way for the reviewer to see the
/// result synchronously, for no benefit here since portal calls are already
/// bounded by Polly's timeout/circuit breaker (ADR-008). "Approved survives even
/// if execution fails" (ADR-010, rule 4) is achieved by never letting the
/// executor's exception propagate (see ApprovalExecutionCoordinator.ExecuteSafelyAsync) —
/// so by the time this handler returns, Status is always a real terminal-ish
/// outcome (Executed or ExecutionFailed) and TransactionBehavior commits the
/// whole thing as one unit; only a genuine infrastructure failure (e.g. the
/// database itself) rolls back to Pending, which is the correct outcome for that
/// case too.
/// </summary>
internal sealed class ApproveApprovalRequestCommandHandler(
    ApprovalsDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    ApprovalExecutionCoordinator coordinator)
    : ICommandHandler<ApproveApprovalRequestCommand, ApprovalDecisionResult>
{
    public async Task<ApprovalDecisionResult> HandleAsync(ApproveApprovalRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await dbContext.ApprovalRequests.FindAsync([command.ApprovalRequestId], cancellationToken)
            ?? throw new ResourceNotFoundException("درخواست تأیید پیدا نشد.");

        // Fail fast on an already-terminal request before touching permissions or
        // the portal (ADR-010, rule 3).
        request.EnsurePending();

        var reviewer = httpContextAccessor.HttpContext!.User;
        var reviewerUserId = ApprovalActorInfo.RequireUserId(httpContextAccessor.HttpContext);

        // ADR-010, rule 2: re-checked here, before any mutation — if the
        // reviewer's access changed since the request was filed, the request is
        // rejected outright, not executed.
        var executor = await coordinator.ResolveAndAuthorizeAsync(request, reviewer, cancellationToken);

        request.MarkApproved(reviewerUserId, coordinator.UtcNow, command.Note);
        await SaveWithConcurrencyCheckAsync(cancellationToken);

        var (ipAddress, userAgent) = ApprovalActorInfo.RequestInfo(httpContextAccessor.HttpContext);
        var actorRoleAtTime = ApprovalActorInfo.RoleAtTime(httpContextAccessor.HttpContext);

        await coordinator.WriteAuditAsync(request, "ApprovalApproved", reviewerUserId, actorRoleAtTime, ipAddress, userAgent, cancellationToken);

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
