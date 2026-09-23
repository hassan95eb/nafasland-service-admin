namespace NafasLand.Admin.Modules.Approvals.Jobs;

/// <summary>ADR-010, rule 6: a Pending request older than 7 days expires automatically.</summary>
internal static class ApprovalExpiryPolicy
{
    public static readonly TimeSpan ExpiryPeriod = TimeSpan.FromDays(7);

    public const string ExpireRecurringJobId = "approvals-expire-pending";

    /// <summary>Once a day at 03:00 — same off-peak convention as Auditing's purge job.</summary>
    public const string ExpireCronExpression = "0 3 * * *";
}
