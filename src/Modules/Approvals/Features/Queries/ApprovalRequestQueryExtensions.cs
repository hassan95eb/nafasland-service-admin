using System.Globalization;
using NafasLand.Admin.Modules.Approvals.Persistence;

namespace NafasLand.Admin.Modules.Approvals.Features.Queries;

/// <summary>
/// Keyset pagination (ADR-014's convention, reused here rather than
/// offset/skip): opaque cursor of "{RequestedAt.Ticks}_{Id}", Id as tiebreaker
/// for rows with the same tick — the same shape as Auditing's own
/// AuditLogQueryExtensions, scoped to ApprovalRequest instead.
/// </summary>
internal static class ApprovalRequestQueryExtensions
{
    public static IQueryable<ApprovalRequest> ApplyKeysetCursor(this IQueryable<ApprovalRequest> query, string? cursor)
    {
        if (string.IsNullOrEmpty(cursor) || !TryDecodeCursor(cursor, out var requestedAt, out var id))
        {
            return query;
        }

        return query.Where(request =>
            request.RequestedAt < requestedAt || (request.RequestedAt == requestedAt && request.Id.CompareTo(id) < 0));
    }

    public static string EncodeCursor(ApprovalRequest request) => $"{request.RequestedAt.Ticks}_{request.Id}";

    private static bool TryDecodeCursor(string cursor, out DateTime requestedAt, out Guid id)
    {
        requestedAt = default;
        id = default;

        var parts = cursor.Split('_', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) || !Guid.TryParse(parts[1], out id))
        {
            return false;
        }

        requestedAt = new DateTime(ticks, DateTimeKind.Utc);
        return true;
    }
}
