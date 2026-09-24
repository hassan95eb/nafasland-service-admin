using System.Globalization;
using System.Security.Claims;
using NafasLand.Admin.Modules.Returns.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Returns.Features.Queries;

/// <summary>
/// Keyset pagination (ADR-014) on ApprovedAt DESC with Id as tiebreaker —
/// the same opaque "{Ticks}_{Id}" cursor shape Auditing and Approvals use —
/// plus the "own records unless returns.read.all" scope shared by the list
/// and detail endpoints.
/// </summary>
internal static class ReturnRecordQueryExtensions
{
    public static IQueryable<ReturnRecord> VisibleTo(this IQueryable<ReturnRecord> query, ClaimsPrincipal user)
    {
        if (user.HasClaim(PermissionClaimTypes.Permission, ReturnsPermissions.ReadAll))
        {
            return query;
        }

        var userId = RequireUserId(user);
        return query.Where(record => record.RegisteredByUserId == userId);
    }

    public static IQueryable<ReturnRecord> ApplyKeysetCursor(this IQueryable<ReturnRecord> query, string? cursor)
    {
        if (string.IsNullOrEmpty(cursor) || !TryDecodeCursor(cursor, out var approvedAt, out var id))
        {
            return query;
        }

        return query.Where(record =>
            record.ApprovedAt < approvedAt || (record.ApprovedAt == approvedAt && record.Id.CompareTo(id) < 0));
    }

    public static string EncodeCursor(ReturnRecord record) => $"{record.ApprovedAt.Ticks}_{record.Id}";

    private static Guid RequireUserId(ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        return idClaim is not null && Guid.TryParse(idClaim.Value, out var userId)
            ? userId
            : throw new AuthorizationDeniedException("کاربر احراز هویت‌نشده.");
    }

    private static bool TryDecodeCursor(string cursor, out DateTime approvedAt, out Guid id)
    {
        approvedAt = default;
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

        approvedAt = new DateTime(ticks, DateTimeKind.Utc);
        return true;
    }
}
