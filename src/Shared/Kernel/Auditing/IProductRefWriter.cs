namespace NafasLand.Admin.Shared.Kernel.Auditing;

/// <summary>
/// Keeps ADR-009's ProductRef (last known title per external product id) current,
/// so the per-product history (ADR-014, view 4) still has a name once the product
/// is renamed or gone on the portal. Defined here, implemented in the Auditing
/// module — the same shape as IAuditLogWriter — so Catalog never references
/// Auditing (ADR-004).
/// </summary>
public interface IProductRefWriter
{
    /// <summary>Best-effort: a failure is logged and swallowed, never failing the product write that already reached the portal.</summary>
    Task UpsertAsync(string externalProductId, string title, CancellationToken cancellationToken);
}
