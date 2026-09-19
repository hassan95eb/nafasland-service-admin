using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

/// <summary>Global log view (ADR-014, view 1) — keyset pagination on CreatedAt DESC, not offset.</summary>
internal static class ListAuditLogsEndpoint
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 500;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/audit/logs", async (
                Guid? actorUserId,
                DateTimeOffset? from,
                DateTimeOffset? to,
                string? action,
                string? entityType,
                AuditOutcome? outcome,
                string? cursor,
                int? pageSize,
                AuditingDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var filter = new AuditLogFilter(actorUserId, from, to, action, entityType, outcome);
                var effectivePageSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);

                var rows = await dbContext.AuditLogs
                    .AsNoTracking()
                    .ApplyFilter(filter)
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
                    items = page.Select(AuditLogSummaryDto.FromEntity),
                    nextCursor,
                });
            })
            .RequireAuthorization()
            .RequirePermission(AuditingPermissions.ReadAll);
    }
}

internal sealed record AuditLogSummaryDto(
    Guid Id,
    string CorrelationId,
    Guid? ActorUserId,
    string ActorRoleAtTime,
    string Action,
    string? EntityType,
    string? EntityId,
    string Outcome,
    string? FailureReason,
    DateTime CreatedAt)
{
    public static AuditLogSummaryDto FromEntity(AuditLog log) => new(
        log.Id,
        log.CorrelationId,
        log.ActorUserId,
        log.ActorRoleAtTime,
        log.Action,
        log.EntityType,
        log.EntityId,
        log.Outcome.ToString(),
        log.FailureReason,
        log.CreatedAt);
}
