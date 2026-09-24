using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Auditing.Configuration;
using NafasLand.Admin.Modules.Auditing.Features.Queries;
using NafasLand.Admin.Modules.Auditing.Jobs;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Kernel.Users;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Auditing.Features.ExportAuditLog;

internal sealed class ExportAuditLogCommandHandler(
    AuditingDbContext dbContext,
    IAuditContext auditContext,
    IBackgroundJobClient backgroundJobClient,
    IOptions<AuditExportOptions> exportOptions,
    IUserDirectory userDirectory,
    TimeProvider timeProvider)
    : ICommandHandler<ExportAuditLogCommand, ExportAuditLogResult>
{
    public async Task<ExportAuditLogResult> HandleAsync(ExportAuditLogCommand command, CancellationToken cancellationToken)
    {
        var query = dbContext.AuditLogs.AsNoTracking().ApplyFilter(command.Filter);
        var matchingCount = await query.CountAsync(cancellationToken);

        auditContext.SetAfter(new { command.Filter, command.Format, matchingCount });

        if (matchingCount <= exportOptions.Value.SynchronousRowThreshold)
        {
            var rows = await query.OrderByDescending(log => log.CreatedAt).ToListAsync(cancellationToken);
            var usernames = await userDirectory.GetUsernamesAsync(ActorIds(rows), cancellationToken);
            var (bytes, contentType, fileName) = AuditLogFileGenerator.Generate(rows, usernames, command.Format);
            return new ExportAuditLogResult(IsAsync: false, bytes, contentType, fileName, JobId: null);
        }

        var job = AuditExportJobRecord.CreateQueued(command.Format, timeProvider.GetUtcNow());
        dbContext.AuditExportJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        backgroundJobClient.Enqueue<AuditExportBackgroundJob>(
            handler => handler.RunAsync(job.Id, command.Filter, command.Format, CancellationToken.None));

        return new ExportAuditLogResult(IsAsync: true, FileBytes: null, ContentType: null, FileName: null, job.Id);
    }

    internal static IReadOnlyCollection<Guid> ActorIds(IEnumerable<AuditLog> rows) =>
        rows.Where(log => log.ActorUserId.HasValue).Select(log => log.ActorUserId!.Value).Distinct().ToList();
}
