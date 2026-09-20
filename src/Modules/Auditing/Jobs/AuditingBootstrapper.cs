using Hangfire;
using NafasLand.Admin.Modules.Auditing.Configuration;
using NafasLand.Admin.Modules.Auditing.Contracts;

namespace NafasLand.Admin.Modules.Auditing.Jobs;

internal sealed class AuditingBootstrapper(IRecurringJobManager recurringJobManager) : IAuditingBootstrapper
{
    public void ScheduleRecurringJobs()
    {
        recurringJobManager.AddOrUpdate<AuditLogPurgeJob>(
            AuditRetentionPolicy.PurgeRecurringJobId,
            job => job.RunAsync(CancellationToken.None),
            AuditRetentionPolicy.PurgeCronExpression);
    }
}
