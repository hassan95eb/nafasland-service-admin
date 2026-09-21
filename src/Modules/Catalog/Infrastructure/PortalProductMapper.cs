using System.Globalization;
using System.Text.Json;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;

namespace NafasLand.Admin.Modules.Catalog.Infrastructure;

internal static class PortalProductMapper
{
    public static PortalProductListResult MapList(string json, PortalProductListQuery query)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var products = FindArray(root, "products", "items", "data");
        var items = products.EnumerateArray().Select(MapSummary).ToArray();
        var total = ReadInt(root, "total") ?? items.Length;
        var count = ReadInt(root, "count") ?? items.Length;

        return new PortalProductListResult(items, total, count, query.Page, query.PageSize);
    }

    public static PortalProductDetail MapDetail(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var product = FindObject(root, "product", "data");

        return new PortalProductDetail(
            ReadRequiredString(product, "id"),
            ReadString(product, "title"),
            ReadString(product, "slug"),
            ReadString(product, "url"),
            ReadString(product, "description"),
            ReadString(product, "contents"),
            ReadDecimal(product, "price"),
            ReadTimestamp(product),
            ReadStrings(product, "statuses", "status"),
            ReadTaxonomies(product, "categories"),
            ReadTaxonomies(product, "filters"),
            ReadImages(product),
            ReadVariants(product),
            ReadStrings(product, "relates"),
            ReadStrings(product, "meta_keywords"),
            ReadString(product, "canonical_url"),
            ReadString(product, "version"));
    }

    public static PortalProductVariant MapVariant(string json)
    {
        using var document = JsonDocument.Parse(json);
        var variant = FindObject(document.RootElement, "variant", "data");
        return MapVariant(variant);
    }

    private static PortalProductSummary MapSummary(JsonElement product)
    {
        return new PortalProductSummary(
            ReadRequiredString(product, "id"),
            ReadString(product, "title"),
            ReadString(product, "slug"),
            ReadString(product, "url"),
            ReadDecimal(product, "price"),
            ReadTimestamp(product),
            ReadStrings(product, "statuses", "status"));
    }

    private static IReadOnlyList<PortalTaxonomyValue> ReadTaxonomies(JsonElement product, string propertyName)
    {
        if (!product.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.Object)
            .Select(value => new PortalTaxonomyValue(
                ReadRequiredString(value, "id"),
                ReadString(value, "title"),
                ReadString(value, "url"),
                ReadInt(value, "weight")))
            .ToArray();
    }

    private static IReadOnlyList<PortalProductImage> ReadImages(JsonElement product)
    {
        if (!product.TryGetProperty("images", out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.Object)
            .Select(value => new PortalProductImage(
                ReadRequiredString(value, "path"),
                ReadString(value, "title")))
            .ToArray();
    }

    private static IReadOnlyList<PortalProductVariant> ReadVariants(JsonElement product)
    {
        if (!product.TryGetProperty("variants", out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.Object)
            .Select(MapVariant)
            .ToArray();
    }

    private static PortalProductVariant MapVariant(JsonElement value)
    {
        return new PortalProductVariant(
            ReadRequiredString(value, "id"),
            ReadString(value, "product_id"),
            ReadString(value, "sku"),
            ReadDecimal(value, "price"),
            ReadDecimal(value, "compare_price"),
            ReadInt(value, "stock"),
            ReadDecimal(value, "shipping"),
            ReadDecimal(value, "tax"));
    }

    private static DateTime? ReadTimestamp(JsonElement product)
    {
        var timestamp = ReadLong(product, "timestamp");
        if (timestamp is null)
        {
            foreach (var propertyName in new[] { "created_at", "created", "date" })
            {
                if (product.TryGetProperty(propertyName, out var date) && date.ValueKind == JsonValueKind.Object)
                {
                    timestamp = ReadLong(date, "timestamp");
                    if (timestamp is not null)
                    {
                        break;
                    }
                }
            }
        }

        return timestamp is null ? null : DateTimeOffset.FromUnixTimeSeconds(timestamp.Value).UtcDateTime;
    }

    private static IReadOnlyList<string> ReadStrings(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            return values.EnumerateArray()
                .Select(ReadScalarString)
                .Where(value => value is not null)
                .Cast<string>()
                .ToArray();
        }

        return [];
    }

    private static JsonElement FindArray(JsonElement root, params string[] propertyNames)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root;
        }

        foreach (var propertyName in propertyNames)
        {
            if (root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array)
            {
                return value;
            }
        }

        throw new JsonException("پاسخ فهرست پرتال فاقد آرایهٔ محصول است.");
    }

    private static JsonElement FindObject(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Object)
            {
                return value;
            }
        }

        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("id", out _))
        {
            return root;
        }

        throw new JsonException("پاسخ جزئیات پرتال فاقد محصول است.");
    }

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        return ReadString(element, propertyName)
            ?? throw new JsonException($"فیلد الزامی {propertyName} در پاسخ پرتال وجود ندارد.");
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? ReadScalarString(value) : null;
    }

    private static string? ReadScalarString(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };
    }

    private static decimal? ReadDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String
            && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out number)
                ? number
                : null;
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        var value = ReadLong(element, propertyName);
        return value is >= int.MinValue and <= int.MaxValue ? (int)value.Value : null;
    }

    private static long? ReadLong(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String
            && long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
                ? number
                : null;
    }
}
