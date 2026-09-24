namespace NafasLand.Admin.Modules.Auditing.Persistence;

/// <summary>
/// ADR-009: keeps old AuditLog rows from becoming an unlabeled "product 8421"
/// once the product itself is gone/renamed on the portal. Written by
/// ProductRefWriter after every successful product create/update in Catalog.
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

    public static ProductRef Create(string externalProductId, string title, DateTime syncedAtUtc) => new()
    {
        ExternalProductId = externalProductId,
        LastKnownTitle = title,
        LastSyncedAt = syncedAtUtc,
    };

    public void Refresh(string title, DateTime syncedAtUtc)
    {
        LastKnownTitle = title;
        LastSyncedAt = syncedAtUtc;
    }
}
