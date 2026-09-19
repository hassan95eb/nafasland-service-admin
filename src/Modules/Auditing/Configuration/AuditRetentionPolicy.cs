namespace NafasLand.Admin.Modules.Auditing.Configuration;

/// <summary>
/// ADR-015: 6-month retention. The purge schedule (daily, 03:00 UTC) is an
/// arbitrary low-traffic pick per the prompt's own instruction — freely
/// changeable, not a load-tested decision.
/// </summary>
internal static class AuditRetentionPolicy
{
    public static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(180);

    public const string PurgeCronExpression = "0 3 * * *";

    public const string PurgeRecurringJobId = "audit-log-purge";
}
