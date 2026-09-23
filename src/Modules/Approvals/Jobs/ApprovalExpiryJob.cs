using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Approvals.Jobs;

/// <summary>
/// Hangfire recurring job (ADR-010, rule 6), same shape as Auditing's own
/// AuditLogPurgeJob: a plain class, not a command handler, so it can run with no
/// HTTP request/actor behind it — ExpiredEvent's actor is "System", exactly like
/// AuditPurged.
/// </summary>
internal sealed class ApprovalExpiryJob(ApprovalsDbContext dbContext, IAuditLogWriter auditLogWriter, TimeProvider timeProvider)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime - ApprovalExpiryPolicy.ExpiryPeriod;

        var expired = await dbContext.ApprovalRequests
            .Where(request => request.Status == ApprovalRequestStatus.Pending && request.RequestedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return;
        }

        foreach (var request in expired)
        {
            request.MarkExpired();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var request in expired)
        {
            var entry = new AuditLogEntry(
                CorrelationId: $"system-approval-expiry:{request.Id}",
                ActorUserId: null,
                OnBehalfOfUserId: request.RequestedByUserId,
                ActorRoleAtTime: "System",
                Action: "ApprovalExpired",
                EntityType: request.TargetEntityType,
                EntityId: request.TargetEntityId,
                BeforeJson: null,
                AfterJson: null,
                ChangedFields: null,
                Outcome: AuditOutcome.Success,
                UpstreamStatus: null,
                FailureReason: null,
                IpAddress: null,
                UserAgent: null);

            await auditLogWriter.WriteAsync(entry, cancellationToken);
        }
    }
}
