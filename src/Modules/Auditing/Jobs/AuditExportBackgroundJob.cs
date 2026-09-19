using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Configuration;
using NafasLand.Admin.Modules.Auditing.Features.ExportAuditLog;
using NafasLand.Admin.Modules.Auditing.Features.Queries;
using NafasLand.Admin.Modules.Auditing.Persistence;

namespace NafasLand.Admin.Modules.Auditing.Jobs;

/// <summary>
/// One-off Hangfire job for the >25,000-row export path. Not command dispatch
/// (no ICommand, no mandatory pipeline) — the ExportAuditLogCommand that queued
/// it already produced its own AuditExported record the moment it enqueued this
/// job (ADR-014); this job only has to produce the file.
/// </summary>
internal sealed class AuditExportBackgroundJob(AuditingDbContext dbContext, TimeProvider timeProvider)
{
    public async Task RunAsync(Guid jobId, AuditLogFilter filter, string format, CancellationToken cancellationToken)
    {
        var job = await dbContext.AuditExportJobs.SingleAsync(j => j.Id == jobId, cancellationToken);
        job.MarkProcessing();
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var rows = await dbContext.AuditLogs
                .AsNoTracking()
                .ApplyFilter(filter)
                .OrderByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);

            var (bytes, _, fileName) = AuditLogFileGenerator.Generate(rows, format);

            Directory.CreateDirectory(AuditFilePaths.ExportDirectory);
            var filePath = Path.Combine(AuditFilePaths.ExportDirectory, $"{jobId}-{fileName}");
            await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);

            job.MarkCompleted(filePath, timeProvider.GetUtcNow());
        }
        catch (Exception exception)
        {
            job.MarkFailed(exception.Message, timeProvider.GetUtcNow());
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
