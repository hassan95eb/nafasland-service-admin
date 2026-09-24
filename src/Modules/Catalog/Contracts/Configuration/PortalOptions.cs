using System.ComponentModel.DataAnnotations;

namespace NafasLand.Admin.Modules.Catalog.Contracts.Configuration;

/// <summary>
/// Catalog's own keys in the shared <c>Portal</c> section (ADR-029). The
/// connection, token, rate-limit and resilience keys of the same section moved
/// to Shared.Infrastructure's PortalConnectionOptions (ADR-054), so the config
/// keys themselves are unchanged.
/// </summary>
public sealed class PortalOptions
{
    public const string SectionName = "Portal";

    [Required(AllowEmptyStrings = false, ErrorMessage = "شناسهٔ محصول تستی تعریف نشده است (ADR-029).")]
    public string TestProductId { get; init; } = string.Empty;

    public bool AllowProductCreation { get; init; }

    /// <summary>
    /// ADR-029 guard: when true (the default), every write is limited to
    /// <see cref="TestProductId"/>. Setting it to false opens writes to every
    /// product on the portal, so it must be turned off explicitly and only
    /// where editing live products is intended.
    /// </summary>
    public bool RestrictWritesToTestProduct { get; init; } = true;

    public bool IsWriteAllowed(string? productId) =>
        !RestrictWritesToTestProduct || string.Equals(productId, TestProductId, StringComparison.Ordinal);
}
