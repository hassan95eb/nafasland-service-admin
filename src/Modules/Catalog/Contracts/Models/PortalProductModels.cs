namespace NafasLand.Admin.Modules.Catalog.Contracts.Models;

public sealed record PortalProductListQuery(int Page, int PageSize, string? Keywords, string? Sorting);

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
    string? Caption,
    string? Slug,
    string? Url,
    string? Description,
    IReadOnlyList<PortalNameValue> Contents,
    bool CommentingEnabled,
    IReadOnlyList<PortalNameValue> Fields,
    IReadOnlyList<PortalAttribute> Attributes,
    decimal? Price,
    DateTime? PublishedAtUtc,
    DateTime? CreatedAtUtc,
    IReadOnlyList<string> Statuses,
    IReadOnlyList<PortalTaxonomyValue> Categories,
    IReadOnlyList<PortalTaxonomyValue> Filters,
    string? Image,
    IReadOnlyList<PortalProductImage> Images,
    IReadOnlyList<PortalProductVariant> Variants,
    IReadOnlyList<long> Relates,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? MetaRobots,
    string? Redirect,
    string? CanonicalUrl,
    string? Version,
    bool IsStale = false,
    DateTimeOffset? AsOfUtc = null)
{
    public bool IsPending => Statuses.Contains("pending", StringComparer.OrdinalIgnoreCase);
    public bool IsAvailable => Statuses.Contains("available", StringComparer.OrdinalIgnoreCase);
}

public sealed record PortalNameValue(string Name, string? Value);
public sealed record PortalAttribute(string Name, IReadOnlyList<string> Value);
public sealed record PortalTaxonomyValue(string Id, string? Title, string? Url, int? Weight);
public sealed record PortalProductImage(string Path, string? Title);

public sealed record PortalProductVariant(
    string? Id,
    string? ProductId,
    string? Title,
    decimal? Price,
    decimal? ComparePrice,
    decimal? Tax,
    decimal? Shipping,
    decimal? Weight,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    int? Stock,
    int? Minimum,
    int? Maximum,
    string? Sku,
    string? Image,
    string? Type,
    IReadOnlyList<string> Status,
    IReadOnlyList<string> Files);

public sealed record PortalVariantPatch(decimal? Price, int? Stock);

public sealed record PortalProductWriteModel(
    string Title,
    string? Caption,
    string? Description,
    IReadOnlyList<PortalNameValue> Contents,
    bool CommentingEnabled,
    IReadOnlyList<PortalNameValue> Fields,
    string? Slug,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? MetaRobots,
    string? Redirect,
    string? CanonicalUrl,
    string? Image,
    IReadOnlyList<string>? Images,
    IReadOnlyList<long> Categories,
    IReadOnlyList<long> Filters,
    IReadOnlyList<long> Relates,
    IReadOnlyList<PortalAttribute> Attributes,
    IReadOnlyList<PortalProductVariant> Variants,
    IReadOnlyList<string> Status,
    DateTime? Published);

public sealed record PortalProductCreateResult(string Id);
public sealed record PortalProductUpdateResult(string? Version);

public sealed record PortalTaxonomyResult<T>(
    IReadOnlyList<T> Items,
    bool IsStale = false,
    DateTimeOffset? AsOfUtc = null);

public sealed record PortalCategoryNode(
    string Id,
    string? Title,
    string? Type,
    IReadOnlyList<PortalCategoryNode> Children);

public sealed record PortalFilterGroup(
    string Id,
    string? Title,
    int? Weight,
    IReadOnlyList<PortalTaxonomyValue> Values);
