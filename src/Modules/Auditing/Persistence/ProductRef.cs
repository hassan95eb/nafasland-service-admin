namespace NafasLand.Admin.Modules.Auditing.Persistence;

/// <summary>
/// ADR-009: keeps old AuditLog rows from becoming an unlabeled "product 8421"
/// once the product itself is gone/renamed on the portal. Nothing populates this
/// table yet (no Catalog module exists); it only needs to exist so the
/// per-product history view (Features/Queries) works immediately once a later
/// step starts writing to it.
/// </summary>
internal sealed class ProductRef
{
    private ProductRef()
    {
        ExternalProductId = string.Empty;
        LastKnownTitle = string.Empty;
    }

    public string ExternalProductId { get; private set; }

    public string LastKnownTitle { get; private set; }

    public DateTime LastSyncedAt { get; private set; }
}
