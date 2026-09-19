namespace NafasLand.Admin.Shared.Kernel.Persistence;

/// <summary>
/// Result of checking a DbContext for pending migrations.
/// </summary>
public sealed record MigrationCheckResult(string ContextName, IReadOnlyList<string> PendingMigrations)
{
    public bool HasPendingMigrations => PendingMigrations.Count > 0;
}

/// <summary>
/// Each module with a DbContext registers one of these so startup can check that
/// no migration is pending (ADR-041). Migrations are never applied automatically;
/// this only checks.
/// </summary>
public interface IMigrationCheck
{
    Task<MigrationCheckResult> CheckAsync(CancellationToken cancellationToken);
}
