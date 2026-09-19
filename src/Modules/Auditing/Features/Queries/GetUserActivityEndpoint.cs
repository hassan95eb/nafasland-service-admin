using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

/// <summary>ADR-014, view 2: one admin's activity — a counted-by-Outcome summary for the window plus a keyset timeline.</summary>
internal static class GetUserActivityEndpoint
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 500;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/audit/users/{userId:guid}", async (
                Guid userId,
                DateTimeOffset? from,
                DateTimeOffset? to,
                string? cursor,
                int? pageSize,
                AuditingDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var windowQuery = dbContext.AuditLogs.AsNoTracking().Where(log => log.ActorUserId == userId);
                if (from is { } fromValue)
                {
                    windowQuery = windowQuery.Where(log => log.CreatedAt >= fromValue.UtcDateTime);
                }

                if (to is { } toValue)
                {
                    windowQuery = windowQuery.Where(log => log.CreatedAt <= toValue.UtcDateTime);
                }

                var countsByOutcome = await windowQuery
                    .GroupBy(log => log.Outcome)
                    .Select(group => new { Outcome = group.Key.ToString(), Count = group.Count() })
                    .ToListAsync(cancellationToken);

                var effectivePageSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
                var rows = await windowQuery
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
                    userId,
                    countsByOutcome,
                    timeline = new
                    {
                        items = page.Select(AuditLogSummaryDto.FromEntity),
                        nextCursor,
                    },
                });
            })
            .RequireAuthorization()
            .RequirePermission(AuditingPermissions.ReadAll);
    }
}
