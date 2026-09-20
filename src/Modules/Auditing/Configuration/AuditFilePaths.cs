namespace NafasLand.Admin.Modules.Auditing.Configuration;

/// <summary>Both under backups/ (already gitignored, ADR-040) — never part of the repository or a build artifact.</summary>
internal static class AuditFilePaths
{
    public const string ExportDirectory = "backups/audit-exports";
    public const string ArchiveDirectory = "backups/audit-archive";
}
