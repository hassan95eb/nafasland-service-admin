using Hangfire;
using NafasLand.Admin.Modules.Catalog.Contracts;

namespace NafasLand.Admin.Modules.Catalog.Jobs;

internal sealed class CatalogBootstrapper(IRecurringJobManager recurringJobManager) : ICatalogBootstrapper
{
    public void ScheduleRecurringJobs()
    {
        recurringJobManager.AddOrUpdate<IdempotencyPurgeJob>(
            IdempotencyRetentionPolicy.PurgeRecurringJobId,
            job => job.RunAsync(CancellationToken.None),
            IdempotencyRetentionPolicy.PurgeCronExpression);
    }
}
