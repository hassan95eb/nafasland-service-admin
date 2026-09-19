namespace NafasLand.Admin.Modules.Auditing;

/// <summary>Permission keys for the Auditing module (ADR-005), both IsSuperAdminOnly (ADR-014): Admin gets neither, and no audit.read.own is ever defined.</summary>
internal static class AuditingPermissions
{
    public const string ReadAll = "audit.read.all";
    public const string Export = "audit.export";
}
