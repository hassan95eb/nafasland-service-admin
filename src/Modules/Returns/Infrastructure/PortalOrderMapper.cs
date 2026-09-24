using System.Globalization;
using System.Text.Json;

namespace NafasLand.Admin.Modules.Returns.Infrastructure;

/// <summary>
/// Reads an order response field by field instead of deserializing it into a
/// DTO, so personal data (contact.mobile, address, zipcode, ip, payments,
/// user.username, national_code, …) is never materialized anywhere — it only
/// exists in the raw response buffer, which is dropped right after this call
/// (ADR-054). The creation time comes from <c>created.timestamp</c>, never
/// from the Jalali strings next to it (ADR-035).
/// </summary>
internal static class PortalOrderMapper
{
    /// <returns>null when the portal answered <c>success: false</c> or without an <c>order</c> object.</returns>
    public static PortalOrder? Map(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False)
        {
            return null;
        }

        if (!root.TryGetProperty("order", out var order) || order.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var orderId = ReadInt64(order, "id") ?? throw new JsonException("order.id is missing.");

        return new PortalOrder(
            orderId,
            ReadStrings(order, "status"),
            ReadCustomerName(order),
            ReadDecimal(order, "subtotal"),
            ReadDecimal(order, "shipping"),
            ReadDecimal(order, "discount"),
            ReadDecimal(order, "tax"),
            ReadDecimal(order, "price"),
            ReadTimestamp(order, "created"),
            ReadItems(order));
    }

    private static string? ReadCustomerName(JsonElement order)
    {
        var contactName = Child(order, "contact") is { } contact ? ReadString(contact, "name") : null;
        if (!string.IsNullOrWhiteSpace(contactName))
        {
            return contactName.Trim();
        }

        var userName = Child(order, "user") is { } user ? ReadString(user, "name") : null;
        return string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();
    }

    private static IReadOnlyList<PortalOrderItem> ReadItems(JsonElement order)
    {
        if (!order.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return items.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Object)
            .Select(item => new PortalOrderItem(
                Child(item, "product") is { } product ? ReadInt64(product, "id") : null,
                Child(item, "variant") is { } variant ? ReadInt64(variant, "id") : null,
                ReadString(item, "title"),
                ReadDecimal(item, "price"),
                (int)(ReadInt64(item, "quantity") ?? 0)))
            .ToList();
    }

    private static DateTime? ReadTimestamp(JsonElement order, string name)
    {
        var seconds = Child(order, name) is { } created ? ReadInt64(created, "timestamp") : null;
        return seconds is { } value ? DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime : null;
    }

    private static JsonElement? Child(JsonElement element, string name) =>
        element.TryGetProperty(name, out var child) && child.ValueKind == JsonValueKind.Object ? child : null;

    private static IReadOnlyList<string> ReadStrings(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => value.GetString()!)
            .ToList();
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static decimal? ReadDecimal(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    private static long? ReadInt64(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var number) => number,
            JsonValueKind.String when long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }
}
