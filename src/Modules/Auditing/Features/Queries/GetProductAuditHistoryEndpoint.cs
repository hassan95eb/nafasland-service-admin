using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Kernel.Users;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

/// <summary>
/// ADR-014, view 4. A product's history is three kinds of record:
/// 1. EntityType = Product on the product itself (create/edit, and product-level
///    approval events, which already target the product).
/// 2. Any record whose Parent is this product — variant price/stock changes
///    written after ParentEntity existed.
/// 3. Variant records by variant id (<paramref name="variantId"/>, sent by the
///    caller from the product it already loaded). This is what reaches variant
///    rows written before ParentEntity existed, and variant-delete approval events
///    (the generic Approvals module only knows the variant as its target) — but
///    only for variants that still exist on the portal.
/// Auditing cannot look the variant ids up itself: that would mean referencing
/// Catalog (ADR-004).
/// </summary>
internal static class GetProductAuditHistoryEndpoint
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 500;
    private const int MaxVariantIds = 200;

    private const string ProductEntityType = "Product";

    // "ProductVariant" is Catalog's own command; "Variant" is what the product
    // page files variant-delete approval requests under.
    private const string VariantEntityType = "ProductVariant";
    private const string ApprovalVariantEntityType = "Variant";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/audit/products/{externalProductId}", async (
                string externalProductId,
                string? cursor,
                int? pageSize,
                [FromQuery] string[]? variantId,
                AuditingDbContext dbContext,
                IUserDirectory userDirectory,
                CancellationToken cancellationToken) =>
            {
                var productRef = await dbContext.ProductRefs
                    .AsNoTracking()
                    .SingleOrDefaultAsync(p => p.ExternalProductId == externalProductId, cancellationToken);

                var variantIds = (variantId ?? [])
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .Take(MaxVariantIds)
                    .ToList();

                var effectivePageSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
                var rows = await dbContext.AuditLogs
                    .AsNoTracking()
                    .ForProduct(externalProductId, variantIds)
                    .ApplyKeysetCursor(cursor)
                    .OrderByDescending(log => log.CreatedAt)
                    .ThenByDescending(log => log.Id)
                    .Take(effectivePageSize + 1)
                    .ToListAsync(cancellationToken);

                var page = await AuditLogPageDto.CreateAsync(rows, effectivePageSize, userDirectory, cancellationToken);

                return TypedResults.Ok(new ProductAuditHistoryDto(
                    externalProductId,
                    productRef?.LastKnownTitle,
                    productRef?.LastSyncedAt,
                    page.Items,
                    page.NextCursor));
            })
            .RequireAuthorization()
            .RequirePermission(AuditingPermissions.ReadAll);
    }

    internal static IQueryable<AuditLog> ForProduct(this IQueryable<AuditLog> query, string productId, IReadOnlyCollection<string> variantIds)
    {
        if (variantIds.Count == 0)
        {
            return query.Where(log =>
                (log.EntityType == ProductEntityType && log.EntityId == productId)
                || (log.ParentEntityType == ProductEntityType && log.ParentEntityId == productId));
        }

        return query.Where(log =>
            (log.EntityType == ProductEntityType && log.EntityId == productId)
            || (log.ParentEntityType == ProductEntityType && log.ParentEntityId == productId)
            || ((log.EntityType == VariantEntityType || log.EntityType == ApprovalVariantEntityType)
                && variantIds.Contains(log.EntityId!)));
    }
}
