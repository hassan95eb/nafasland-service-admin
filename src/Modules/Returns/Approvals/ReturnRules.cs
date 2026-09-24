using System.Globalization;
using NafasLand.Admin.Modules.Returns.Infrastructure;

namespace NafasLand.Admin.Modules.Returns.Approvals;

/// <summary>
/// The filing rules of ADR-054, checked both when the request is filed and
/// again when it is executed. Messages are Persian and safe to show the user.
/// </summary>
internal static class ReturnRules
{
    public const string OrderNotFound = "سفارشی با این شناسه پیدا نشد";
    public const string AlreadyRegistered = "برای این سفارش قبلاً مرجوعی ثبت شده است.";

    // Iran has had no daylight saving since 2022, so Tehran is a fixed UTC+03:30
    // (the same assumption the frontend's Jalali helpers make).
    private static readonly TimeSpan TehranOffset = TimeSpan.FromMinutes(210);

    public static DateOnly TehranDate(DateTime utc) => DateOnly.FromDateTime(utc + TehranOffset);

    public static DateOnly TehranToday(TimeProvider timeProvider) => TehranDate(timeProvider.GetUtcNow().UtcDateTime);

    /// <returns>A Persian reason when the order cannot be returned; null when it can.</returns>
    public static string? CheckOrderEligibility(PortalOrder order)
    {
        if (order.Statuses.Contains("canceled", StringComparer.OrdinalIgnoreCase))
        {
            return "این سفارش لغو شده است و ثبت مرجوعی برای آن ممکن نیست.";
        }

        if (!order.Statuses.Contains("paid", StringComparer.OrdinalIgnoreCase))
        {
            return "این سفارش پرداخت نشده است و ثبت مرجوعی برای آن ممکن نیست.";
        }

        return null;
    }

    /// <summary>Only the "not before the order" half; "not in the future" is in the payload validator.</summary>
    public static string? CheckReturnDate(DateOnly returnDate, PortalOrder order)
    {
        if (order.CreatedAtUtc is { } createdAt && returnDate < TehranDate(createdAt))
        {
            return "تاریخ عودت نمی‌تواند پیش از تاریخ ثبت سفارش باشد.";
        }

        return null;
    }

    public static string FormatJalali(DateOnly date)
    {
        var calendar = new PersianCalendar();
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{calendar.GetYear(dateTime):0000}/{calendar.GetMonth(dateTime):00}/{calendar.GetDayOfMonth(dateTime):00}");
    }
}
