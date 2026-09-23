using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Approvals.Features.Queries;

/// <summary>
/// A read-only query endpoint (no Command/Handler, same as Auditing's list
/// endpoints — ADR-006's mandatory pipeline only applies to mutating commands).
/// "approval.read.own" (ADR-010) is not a grantable permission under the
/// default-closed model since every role is meant to have it — instead, a user
/// without approvals.read.all is silently restricted to their own requests
/// rather than rejected outright, and `mine=false` from such a user is ignored
/// rather than honoured.
/// </summary>
internal static class ListApprovalRequestsEndpoint
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/approvals", async (
                HttpContext httpContext,
                string? status,
                string? type,
                bool? mine,
                string? targetEntityType,
                string? targetEntityId,
                string? cursor,
                int? pageSize,
                ApprovalsDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var currentUserId = ApprovalActorInfo.RequireUserId(httpContext);
                var canReadAll = httpContext.User.HasClaim(PermissionClaimTypes.Permission, ApprovalsPermissions.ReadAll);

                // A query scoped to one specific target entity (the "does this
                // product/variant have a Pending request?" badge lookup) is an
                // existence check any authenticated user may run — it is not the
                // same thing as browsing the cartable, so it is exempt from the
                // own/read-all restriction below.
                var isTargetedLookup = !string.IsNullOrWhiteSpace(targetEntityType) && !string.IsNullOrWhiteSpace(targetEntityId);
                var restrictToOwn = !isTargetedLookup && (!canReadAll || mine == true);

                var query = dbContext.ApprovalRequests.AsNoTracking().AsQueryable();
                if (restrictToOwn)
                {
                    query = query.Where(request => request.RequestedByUserId == currentUserId);
                }

                if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ApprovalRequestStatus>(status, ignoreCase: true, out var parsedStatus))
                {
                    query = query.Where(request => request.Status == parsedStatus);
                }

                if (!string.IsNullOrWhiteSpace(type))
                {
                    query = query.Where(request => request.RequestType == type);
                }

                // Lets a product/variant page ask "is there a Pending request on
                // me?" without a dedicated endpoint (used for the badge in
                // product-details.tsx) — not part of ADR-010's original endpoint
                // list, added because the step prompt asks for it explicitly.
                if (!string.IsNullOrWhiteSpace(targetEntityType))
                {
                    query = query.Where(request => request.TargetEntityType == targetEntityType);
                }

                if (!string.IsNullOrWhiteSpace(targetEntityId))
                {
                    query = query.Where(request => request.TargetEntityId == targetEntityId);
                }

                var effectivePageSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
                var rows = await query
                    .ApplyKeysetCursor(cursor)
                    .OrderByDescending(request => request.RequestedAt)
                    .ThenByDescending(request => request.Id)
                    .Take(effectivePageSize + 1)
                    .ToListAsync(cancellationToken);

                var hasMore = rows.Count > effectivePageSize;
                var page = hasMore ? rows.Take(effectivePageSize).ToList() : rows;
                var nextCursor = hasMore ? ApprovalRequestQueryExtensions.EncodeCursor(page[^1]) : null;

                return Results.Ok(new { items = page.Select(ApprovalRequestSummaryDto.FromEntity), nextCursor });
            })
            .RequireAuthorization();
    }
}
