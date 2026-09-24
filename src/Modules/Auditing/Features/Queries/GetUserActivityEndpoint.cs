using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Kernel.Users;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

/// <summary>
/// ADR-014, view 2: one admin's activity — counts for the window (by Outcome and
/// by raw Action) plus a keyset timeline. Grouping Actions into display
/// categories ("create", "edit", …) is the frontend's job, not a closed enum here,
/// so a new Action from a later module shows up without touching this endpoint.
/// </summary>
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
                IUserDirectory userDirectory,
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

                var outcomeCounts = await windowQuery
                    .GroupBy(log => log.Outcome)
                    .Select(group => new { group.Key, Count = group.Count() })
                    .ToListAsync(cancellationToken);

                var actionCounts = await windowQuery
                    .GroupBy(log => new { log.Action, log.Outcome })
                    .Select(group => new { group.Key.Action, group.Key.Outcome, Count = group.Count() })
                    .ToListAsync(cancellationToken);

                var effectivePageSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
                var rows = await windowQuery
                    .ApplyKeysetCursor(cursor)
                    .OrderByDescending(log => log.CreatedAt)
                    .ThenByDescending(log => log.Id)
                    .Take(effectivePageSize + 1)
                    .ToListAsync(cancellationToken);

                var usernames = await userDirectory.GetUsernamesAsync([userId], cancellationToken);
                var timeline = await AuditLogPageDto.CreateAsync(rows, effectivePageSize, userDirectory, cancellationToken);

                return TypedResults.Ok(new UserActivityDto(
                    userId,
                    AuditLogSummaryDto.UsernameOf(userId, usernames),
                    outcomeCounts.Select(count => new OutcomeCountDto(count.Key.ToString(), count.Count)).ToList(),
                    actionCounts
                        .OrderByDescending(count => count.Count)
                        .Select(count => new ActionCountDto(count.Action, count.Outcome.ToString(), count.Count))
                        .ToList(),
                    timeline));
            })
            .RequireAuthorization()
            .RequirePermission(AuditingPermissions.ReadAll);
    }
}
