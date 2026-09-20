namespace NafasLand.Admin.Shared.Kernel.Auditing;

/// <summary>
/// A command implementing this interface is automatically logged by
/// AuditBehavior/AuthorizationBehavior on every outcome (Success/Failed/Denied,
/// ADR-009/ADR-048). A command without this marker (e.g. PingCommand,
/// LoginCommand) is never written to AuditLog — this is opt-in, unlike the
/// default-closed permission model.
/// </summary>
public interface IAuditableCommand
{
    /// <summary>e.g. "UserCreated", "RolePermissionsChanged".</summary>
    string AuditAction { get; }

    /// <summary>e.g. "User", "Role", "Product".</summary>
    string AuditEntityType { get; }
}
