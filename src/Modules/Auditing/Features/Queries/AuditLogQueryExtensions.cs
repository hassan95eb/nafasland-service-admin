using System.Globalization;
using NafasLand.Admin.Modules.Auditing.Persistence;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

/// <summary>
/// Filtering shared by the global log view and ExportAuditLog, and keyset
/// (not offset) pagination (ADR-014) for every paginated view. The cursor is an
/// opaque "{CreatedAt ticks}_{Id}" string — the Id tiebreaker guards against two
/// rows sharing the same CreatedAt tick, which plain CreatedAt-only paging would
/// silently mishandle.
/// </summary>
internal static class AuditLogQueryExtensions
{
    public static IQueryable<AuditLog> ApplyFilter(this IQueryable<AuditLog> query, AuditLogFilter filter)
    {
        if (filter.ActorUserId is { } actorUserId)
        {
            query = query.Where(log => log.ActorUserId == actorUserId);
        }

        if (filter.From is { } from)
        {
            query = query.Where(log => log.CreatedAt >= from.UtcDateTime);
        }

        if (filter.To is { } to)
        {
            query = query.Where(log => log.CreatedAt <= to.UtcDateTime);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(log => log.Action == filter.Action);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            query = query.Where(log => log.EntityType == filter.EntityType);
        }

        if (filter.Outcome is { } outcome)
        {
            query = query.Where(log => log.Outcome == outcome);
        }

        return query;
    }

    public static IQueryable<AuditLog> ApplyKeysetCursor(this IQueryable<AuditLog> query, string? cursor)
    {
        if (string.IsNullOrEmpty(cursor) || !TryDecodeCursor(cursor, out var createdAt, out var id))
        {
            return query;
        }

        return query.Where(log => log.CreatedAt < createdAt || (log.CreatedAt == createdAt && log.Id.CompareTo(id) < 0));
    }

    public static string EncodeCursor(AuditLog log) => $"{log.CreatedAt.Ticks}_{log.Id}";

    private static bool TryDecodeCursor(string cursor, out DateTime createdAt, out Guid id)
    {
        createdAt = default;
        id = default;

        var separatorIndex = cursor.IndexOf('_');
        if (separatorIndex < 0)
        {
            return false;
        }

        var ticksPart = cursor[..separatorIndex];
        var idPart = cursor[(separatorIndex + 1)..];

        if (!long.TryParse(ticksPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks)
            || !Guid.TryParse(idPart, out id))
        {
            return false;
        }

        createdAt = new DateTime(ticks, DateTimeKind.Utc);
        return true;
    }
}
