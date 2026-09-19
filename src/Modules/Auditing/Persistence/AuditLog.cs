using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Auditing.Persistence;

/// <summary>
/// Append-only (ADR-009, ADR-015): no Update method exists on this entity and no
/// endpoint/command ever modifies or deletes a row, not even for SuperAdmin.
/// </summary>
internal sealed class AuditLog
{
    private AuditLog()
    {
        CorrelationId = string.Empty;
        ActorRoleAtTime = string.Empty;
        Action = string.Empty;
    }

    public Guid Id { get; private set; }

    public string CorrelationId { get; private set; }

    public Guid? ActorUserId { get; private set; }

    /// <summary>Reserved for a future on-behalf-of feature; always null in this step.</summary>
    public Guid? OnBehalfOfUserId { get; private set; }

    public string ActorRoleAtTime { get; private set; }

    public string Action { get; private set; }

    public string? EntityType { get; private set; }

    public string? EntityId { get; private set; }

    public string? BeforeJson { get; private set; }

    public string? AfterJson { get; private set; }

    public string? ChangedFields { get; private set; }

    public AuditOutcome Outcome { get; private set; }

    /// <summary>Reserved for when products reach this panel; always null in this step.</summary>
    public int? UpstreamStatus { get; private set; }

    public string? FailureReason { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static AuditLog FromEntry(AuditLogEntry entry, DateTime createdAtUtc)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = entry.CorrelationId,
            ActorUserId = entry.ActorUserId,
            OnBehalfOfUserId = entry.OnBehalfOfUserId,
            ActorRoleAtTime = entry.ActorRoleAtTime,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            BeforeJson = entry.BeforeJson,
            AfterJson = entry.AfterJson,
            ChangedFields = entry.ChangedFields,
            Outcome = entry.Outcome,
            UpstreamStatus = entry.UpstreamStatus,
            FailureReason = entry.FailureReason,
            IpAddress = entry.IpAddress,
            UserAgent = entry.UserAgent,
            CreatedAt = createdAtUtc,
        };
    }
}
