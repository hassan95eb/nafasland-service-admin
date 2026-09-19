namespace NafasLand.Admin.Shared.Kernel.Auditing;

/// <summary>
/// The only place a row is ever inserted into AuditLog (ADR-009, ADR-048).
/// Implemented in the Auditing module (needs its DbContext) and consumed by
/// AuditBehavior/AuthorizationBehavior in Shared.Infrastructure, and directly by
/// Auditing's own background jobs (export, purge) which are not command dispatch.
/// </summary>
public interface IAuditLogWriter
{
    Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken);
}
