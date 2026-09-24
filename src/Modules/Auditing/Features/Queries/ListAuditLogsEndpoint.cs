using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Users;

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
                IUserDirectory userDirectory,
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

                return TypedResults.Ok(await AuditLogPageDto.CreateAsync(rows, effectivePageSize, userDirectory, cancellationToken));
            })
            .RequireAuthorization()
            .RequirePermission(AuditingPermissions.ReadAll);
    }
}
