namespace NafasLand.Admin.Shared.Kernel.Persistence;

/// <summary>
/// نتیجهٔ بررسی مهاجرت معوق یک DbContext.
/// </summary>
public sealed record MigrationCheckResult(string ContextName, IReadOnlyList<string> PendingMigrations)
{
    public bool HasPendingMigrations => PendingMigrations.Count > 0;
}

/// <summary>
/// هر ماژول با DbContext یکی از این‌ها را ثبت می‌کند تا در استارتاپ بررسی شود
/// که مهاجرت معوقی نمانده باشد (ADR-041). مهاجرت خودکار اجرا نمی‌شود؛ فقط بررسی.
/// </summary>
public interface IMigrationCheck
{
    Task<MigrationCheckResult> CheckAsync(CancellationToken cancellationToken);
}
