using Microsoft.AspNetCore.Http;
using NafasLand.Admin.Shared.Infrastructure.Auditing;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// The fifth link in the pipeline (ADR-006, unchanged position — ADR-048). A
/// command not implementing IAuditableCommand passes straight through, exactly
/// as in step 0; nothing about it is logged. For an auditable command, writes
/// Success after a successful next(), or Failed (with the exception's message,
/// never its stack trace) before rethrowing — Denied is written by
/// AuthorizationBehavior instead, since a denied command never reaches here.
/// </summary>
internal sealed class AuditBehavior<TCommand, TResponse>(
    AuditContext auditContext,
    IAuditLogWriter auditLogWriter,
    IHttpContextAccessor httpContextAccessor,
    ICorrelationIdAccessor correlationIdAccessor)
    : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (command is not IAuditableCommand auditable)
        {
            return await next();
        }

        try
        {
            var response = await next();
            await WriteAsync(auditable, AuditOutcome.Success, failureReason: null, cancellationToken);
            return response;
        }
        catch (Exception exception)
        {
            await WriteAsync(auditable, AuditOutcome.Failed, exception.Message, cancellationToken);
            throw;
        }
    }

    private Task WriteAsync(IAuditableCommand auditable, AuditOutcome outcome, string? failureReason, CancellationToken cancellationToken)
    {
        var (actorUserId, actorRoleAtTime, ipAddress, userAgent) = AuditActorInfo.Extract(httpContextAccessor.HttpContext);
        var changedFields = auditContext.ComputeChangedFields();

        var entry = new AuditLogEntry(
            CorrelationId: correlationIdAccessor.CorrelationId,
            ActorUserId: actorUserId,
            OnBehalfOfUserId: null,
            ActorRoleAtTime: actorRoleAtTime,
            Action: auditable.AuditAction,
            EntityType: auditable.AuditEntityType,
            EntityId: auditContext.EntityId,
            BeforeJson: auditContext.BeforeJson,
            AfterJson: auditContext.AfterJson,
            ChangedFields: changedFields.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(changedFields) : null,
            Outcome: outcome,
            UpstreamStatus: null,
            FailureReason: failureReason,
            IpAddress: ipAddress,
            UserAgent: userAgent);

        return auditLogWriter.WriteAsync(entry, cancellationToken);
    }
}
