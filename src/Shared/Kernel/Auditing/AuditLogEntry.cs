namespace NafasLand.Admin.Shared.Kernel.Auditing;

/// <summary>
/// Everything IAuditLogWriter needs to insert one AuditLog row (ADR-009), except
/// Id/CreatedAt which the writer itself fills in.
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
    string? UserAgent);
