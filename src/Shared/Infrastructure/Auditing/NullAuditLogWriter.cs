using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Shared.Infrastructure.Auditing;

/// <summary>
/// Safe default so AuditBehavior/AuthorizationBehavior can be constructed (and
/// tested) without the Auditing module present — registered with TryAddScoped in
/// AddSharedInfrastructure, so AuditingModule's real AuditLogWriter (registered
/// afterwards, when that module is enabled) takes over in the real app.
/// </summary>
internal sealed class NullAuditLogWriter : IAuditLogWriter
{
    public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;
}
