namespace NafasLand.Admin.Modules.Catalog.Jobs;

internal static class IdempotencyRetentionPolicy
{
    public static readonly TimeSpan RetentionPeriod = TimeSpan.FromHours(24);

    public const string PurgeCronExpression = "30 * * * *";

    public const string PurgeRecurringJobId = "catalog-idempotency-purge";
}
