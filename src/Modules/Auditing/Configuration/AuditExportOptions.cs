namespace NafasLand.Admin.Modules.Auditing.Configuration;

/// <summary>
/// The 25,000-row synchronous/async export threshold (ADR-014), made an
/// injectable setting (not a hardcoded constant) specifically so the required
/// test for the >25,000-row path can simulate it at a much lower number instead
/// of generating tens of thousands of rows.
/// </summary>
internal sealed class AuditExportOptions
{
    public const string SectionName = "Auditing:Export";

    public int SynchronousRowThreshold { get; init; } = 25_000;
}
