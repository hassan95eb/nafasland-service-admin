using Hangfire;
using NafasLand.Admin.Modules.Approvals.Contracts;

namespace NafasLand.Admin.Modules.Approvals.Jobs;

internal sealed class ApprovalsBootstrapper(IRecurringJobManager recurringJobManager) : IApprovalsBootstrapper
{
    public void ScheduleRecurringJobs()
    {
        recurringJobManager.AddOrUpdate<ApprovalExpiryJob>(
            ApprovalExpiryPolicy.ExpireRecurringJobId,
            job => job.RunAsync(CancellationToken.None),
            ApprovalExpiryPolicy.ExpireCronExpression);
    }
}
