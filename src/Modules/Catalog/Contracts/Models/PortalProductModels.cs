namespace NafasLand.Admin.Modules.Catalog.Contracts.Models;

public sealed record PortalProductListQuery(
    int Page,
    int PageSize,
    string? Keywords,
    string? Sorting);

public sealed record PortalProductListResult(
    IReadOnlyList<PortalProductSummary> Items,
    int Total,
    int Count,
    int Page,
    int PageSize,
    bool IsStale = false,
    DateTimeOffset? AsOfUtc = null);

public sealed record PortalProductSummary(
    string Id,
    string? Title,
    string? Slug,
    string? Url,
    decimal? Price,
    DateTime? CreatedAtUtc,
    IReadOnlyList<string> Statuses)
{
    public bool IsPending => Statuses.Contains("pending", StringComparer.OrdinalIgnoreCase);

    public bool IsAvailable => Statuses.Contains("available", StringComparer.OrdinalIgnoreCase);
}

public sealed record PortalProductDetail(
    string Id,
    string? Title,
    string? Slug,
    string? Url,
    string? Description,
    string? Contents,
    decimal? Price,
    DateTime? CreatedAtUtc,
    IReadOnlyList<string> Statuses,
    IReadOnlyList<PortalTaxonomyValue> Categories,
    IReadOnlyList<PortalTaxonomyValue> Filters,
    IReadOnlyList<PortalProductImage> Images,
    IReadOnlyList<PortalProductVariant> Variants,
    IReadOnlyList<string> Relates,
    IReadOnlyList<string> MetaKeywords,
    string? CanonicalUrl,
    string? Version,
    bool IsStale = false,
    DateTimeOffset? AsOfUtc = null)
{
    public bool IsPending => Statuses.Contains("pending", StringComparer.OrdinalIgnoreCase);

    public bool IsAvailable => Statuses.Contains("available", StringComparer.OrdinalIgnoreCase);
}

public sealed record PortalTaxonomyValue(
    string Id,
    string? Title,
    string? Url,
    int? Weight);

public sealed record PortalProductImage(
    string Path,
    string? Title);

public sealed record PortalProductVariant(
    string Id,
    string? ProductId,
    string? Sku,
    decimal? Price,
    decimal? ComparePrice,
    int? Stock,
    decimal? Shipping,
    decimal? Tax);

public sealed record PortalVariantPatch(decimal? Price, int? Stock);
