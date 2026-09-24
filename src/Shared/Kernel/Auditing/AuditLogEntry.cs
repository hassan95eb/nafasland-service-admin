namespace NafasLand.Admin.Shared.Kernel.Auditing;

/// <summary>
/// Everything IAuditLogWriter needs to insert one AuditLog row (ADR-009), except
/// Id/CreatedAt which the writer itself fills in. ParentEntityType/ParentEntityId
/// link a child record (e.g. a ProductVariant) to the entity whose history it
/// belongs to (the Product), so per-entity history (ADR-014, view 4) can include
/// it; optional, so every existing writer is unaffected.
/// </summary>
public sealed record AuditLogEntry(
    string CorrelationId,
    Guid? ActorUserId,
    Guid? OnBehalfOfUserId,
    string ActorRoleAtTime,
    string Action,
    string? EntityType,
    string? EntityId,
    string? BeforeJson,
    string? AfterJson,
    string? ChangedFields,
    AuditOutcome Outcome,
    int? UpstreamStatus,
    string? FailureReason,
    string? IpAddress,
    string? UserAgent,
    string? ParentEntityType = null,
    string? ParentEntityId = null);
