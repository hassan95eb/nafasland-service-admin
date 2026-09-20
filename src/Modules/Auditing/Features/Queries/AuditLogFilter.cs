using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

/// <summary>Shared between the global log view and ExportAuditLog, which exports "the same filters" (ADR-014).</summary>
internal sealed record AuditLogFilter(
    Guid? ActorUserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Action,
    string? EntityType,
    AuditOutcome? Outcome);
