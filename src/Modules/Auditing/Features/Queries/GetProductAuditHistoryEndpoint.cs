using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

/// <summary>
/// ADR-014, view 4. No Catalog module exists yet, so this is never exercised
/// with real data in this step — the endpoint and its ProductRef join just need
/// to exist and not 500 when the ref row is missing (ADR-009's own caveat).
/// </summary>
internal static class GetProductAuditHistoryEndpoint
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 500;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/audit/products/{externalProductId}", async (
                string externalProductId,
                string? cursor,
                int? pageSize,
                AuditingDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var productRef = await dbContext.ProductRefs
                    .AsNoTracking()
                    .SingleOrDefaultAsync(p => p.ExternalProductId == externalProductId, cancellationToken);

                var effectivePageSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
                var query = dbContext.AuditLogs
                    .AsNoTracking()
                    .Where(log => log.EntityType == "Product" && log.EntityId == externalProductId);

                var rows = await query
                    .ApplyKeysetCursor(cursor)
                    .OrderByDescending(log => log.CreatedAt)
                    .ThenByDescending(log => log.Id)
                    .Take(effectivePageSize + 1)
                    .ToListAsync(cancellationToken);

                var hasMore = rows.Count > effectivePageSize;
                var page = hasMore ? rows.Take(effectivePageSize).ToList() : rows;
                var nextCursor = hasMore ? AuditLogQueryExtensions.EncodeCursor(page[^1]) : null;

                return Results.Ok(new
                {
                    externalProductId,
                    lastKnownTitle = productRef?.LastKnownTitle,
                    items = page.Select(AuditLogSummaryDto.FromEntity),
                    nextCursor,
                });
            })
            .RequireAuthorization()
            .RequirePermission(AuditingPermissions.ReadAll);
    }
}
