namespace NafasLand.Admin.Modules.Approvals.Contracts;

/// <summary>
/// Called once from Api after the app is built, same spot and for the same
/// reason as Auditing's and Catalog's own bootstrapper (see
/// IAuditingBootstrapper's comment): a static Hangfire call inside
/// ApprovalsModule.RegisterServices would also run during `dotnet ef` design-time
/// discovery, where JobStorage isn't initialized.
/// </summary>
public interface IApprovalsBootstrapper
{
    void ScheduleRecurringJobs();
}
