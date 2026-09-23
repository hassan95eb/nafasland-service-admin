using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Approvals.Tests.Fakes;

internal sealed class RecordingAuditLogWriter : IAuditLogWriter
{
    public List<AuditLogEntry> WrittenEntries { get; } = [];

    public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        WrittenEntries.Add(entry);
        return Task.CompletedTask;
    }
}
