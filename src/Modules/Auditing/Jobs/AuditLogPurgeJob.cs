using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Configuration;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Auditing.Jobs;

/// <summary>
/// Hangfire recurring job (ADR-015): archives then deletes AuditLog rows older
/// than the retention window, then logs the purge itself — directly through
/// IAuditLogWriter, since this is not command dispatch (no pipeline involved).
/// </summary>
internal sealed class AuditLogPurgeJob(AuditingDbContext dbContext, IAuditLogWriter auditLogWriter, TimeProvider timeProvider)
{
    /// <param name="archiveDirectory">
    /// Defaults to AuditFilePaths.ArchiveDirectory; overridable so tests can
    /// point it at a temp directory instead of writing under the real repo.
    /// </param>
    public async Task RunAsync(CancellationToken cancellationToken, string? archiveDirectory = null)
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime - AuditRetentionPolicy.RetentionPeriod;
        var oldRows = await dbContext.AuditLogs
            .Where(log => log.CreatedAt < cutoff)
            .OrderBy(log => log.CreatedAt)
            .ToListAsync(cancellationToken);

        if (oldRows.Count == 0)
        {
            return;
        }

        var effectiveArchiveDirectory = archiveDirectory ?? AuditFilePaths.ArchiveDirectory;
        Directory.CreateDirectory(effectiveArchiveDirectory);
        var archivePath = Path.Combine(
            effectiveArchiveDirectory,
            $"audit-archive-{timeProvider.GetUtcNow():yyyyMMddHHmmss}.jsonl.gz");

        await using (var fileStream = File.Create(archivePath))
        await using (var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal))
        await using (var writer = new StreamWriter(gzipStream))
        {
            foreach (var row in oldRows)
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(row));
            }
        }

        var deletedCount = oldRows.Count;
        var earliestCreatedAt = oldRows[0].CreatedAt;
        var latestCreatedAt = oldRows[^1].CreatedAt;

        dbContext.AuditLogs.RemoveRange(oldRows);
        await dbContext.SaveChangesAsync(cancellationToken);

        var entry = new AuditLogEntry(
            CorrelationId: Guid.NewGuid().ToString(),
            ActorUserId: null,
            OnBehalfOfUserId: null,
            ActorRoleAtTime: "System",
            Action: "AuditPurged",
            EntityType: "AuditLog",
            EntityId: null,
            BeforeJson: null,
            AfterJson: JsonSerializer.Serialize(new { deletedCount, earliestCreatedAt, latestCreatedAt, archivePath }),
            ChangedFields: null,
            Outcome: AuditOutcome.Success,
            UpstreamStatus: null,
            FailureReason: null,
            IpAddress: null,
            UserAgent: null);

        await auditLogWriter.WriteAsync(entry, cancellationToken);
    }
}
