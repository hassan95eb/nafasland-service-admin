using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Approvals.Infrastructure;

/// <summary>
/// Shared by ApproveApprovalRequestCommandHandler and RetryApprovalRequestCommandHandler —
/// both need the same three steps: re-check the reviewer's permission against
/// the resolved executor (ADR-010, rule 2), run the executor without letting any
/// exception escape (ADR-010, rule 4), and write the resulting AuditLog row with
/// both ActorUserId (reviewer) and OnBehalfOfUserId (original requester) set —
/// which the generic IAuditableCommand/AuditBehavior path cannot do, since it
/// always writes OnBehalfOfUserId as null (see AuditBehavior's own comment).
/// Writing directly through IAuditLogWriter here, outside the command pipeline's
/// audit step, is the same pattern AuditLogPurgeJob already uses for events with
/// no single HTTP-request actor.
/// </summary>
internal sealed class ApprovalExecutionCoordinator(
    IApprovalExecutorRegistry registry,
    IAuthorizationService authorizationService,
    IAuditLogWriter auditLogWriter,
    ICorrelationIdAccessor correlationIdAccessor,
    TimeProvider timeProvider)
{
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<IApprovalExecutor> ResolveAndAuthorizeAsync(
        ApprovalRequest request,
        ClaimsPrincipal reviewer,
        CancellationToken cancellationToken)
    {
        var executor = registry.Resolve(request.RequestType)
            ?? throw new BusinessRuleException("نوع درخواست تأیید دیگر در سامانه رجیستر نیست.");

        var authorizationResult = await authorizationService.AuthorizeAsync(
            reviewer,
            resource: null,
            new PermissionRequirement(executor.RequiredPermission));

        if (!authorizationResult.Succeeded)
        {
            throw new AuthorizationDeniedException(
                $"دسترسی {executor.RequiredPermission} لازم است؛ دسترسی تأییدکننده از زمان ثبت درخواست تغییر کرده است.");
        }

        return executor;
    }

    public async Task<Result> ExecuteSafelyAsync(
        IApprovalExecutor executor,
        ApprovalRequest request,
        Guid reviewerUserId,
        CancellationToken cancellationToken)
    {
        var context = new ApprovalContext(
            request.Id,
            reviewerUserId,
            request.RequestedByUserId,
            correlationIdAccessor.CorrelationId,
            request.RequestedAt,
            request.ReviewedAt ?? UtcNow);
        try
        {
            return await executor.ExecuteAsync(request.PayloadJson, context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // ADR-010 rule 4: a failed upstream call must land on ExecutionFailed
            // with the error stored, never bubble out as an unhandled exception —
            // which would also undo the reviewer's decision already saved above.
            return Result.Failure(exception.Message);
        }
    }

    public Task WriteAuditAsync(
        ApprovalRequest request,
        string action,
        Guid actorUserId,
        string actorRoleAtTime,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken,
        string? failureReason = null)
    {
        var entry = new AuditLogEntry(
            // ApprovalRequestId travels inside CorrelationId (an open decision this
            // step made explicitly, reported in the PR description) rather than as
            // a new AuditLogEntry field, since ADR-009's shape is not touched here.
            CorrelationId: $"{correlationIdAccessor.CorrelationId}:approval:{request.Id}",
            ActorUserId: actorUserId,
            OnBehalfOfUserId: request.RequestedByUserId,
            ActorRoleAtTime: actorRoleAtTime,
            Action: action,
            EntityType: request.TargetEntityType,
            EntityId: request.TargetEntityId,
            BeforeJson: null,
            AfterJson: null,
            ChangedFields: null,
            Outcome: failureReason is null ? AuditOutcome.Success : AuditOutcome.Failed,
            UpstreamStatus: null,
            FailureReason: failureReason,
            IpAddress: ipAddress,
            UserAgent: userAgent);

        return auditLogWriter.WriteAsync(entry, cancellationToken);
    }
}
