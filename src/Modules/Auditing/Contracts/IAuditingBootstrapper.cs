namespace NafasLand.Admin.Modules.Auditing.Contracts;

/// <summary>
/// Called once from Api after the app is built (same spot as Identity's own
/// bootstrap). Deliberately NOT done inside AuditingModule.RegisterServices: a
/// static Hangfire call at DI-registration time runs during `dotnet ef`
/// design-time discovery too (which builds a minimal host to find DbContext
/// types) and fails there because JobStorage isn't initialized in that context —
/// this indirection sidesteps that entirely by only running at real app startup.
/// </summary>
public interface IAuditingBootstrapper
{
    void ScheduleRecurringJobs();
}
