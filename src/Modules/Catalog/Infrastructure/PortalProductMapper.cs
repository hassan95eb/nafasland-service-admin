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
        return new PortalProductListResult(
            items,
            ReadInt(root, "total") ?? items.Length,
            ReadInt(root, "count") ?? items.Length,
            query.Page,
            query.PageSize);
    }

    public static PortalProductDetail MapDetail(string json)
    {
        using var document = JsonDocument.Parse(json);
        var product = FindObject(document.RootElement, "product", "data");

        return new PortalProductDetail(
            ReadRequiredString(product, "id"),
            ReadString(product, "title"),
            ReadString(product, "caption"),
            ReadString(product, "slug"),
            ReadString(product, "url"),
            ReadString(product, "description"),
            ReadNameValues(product, "contents"),
            ReadBool(product, "commenting_enabled"),
            ReadNameValues(product, "fields"),
            ReadAttributes(product),
            ReadDecimal(product, "price"),
            ReadTimestamp(product, "published"),
            ReadTimestamp(product, "created", "created_at"),
            ReadStrings(product, "statuses", "status"),
            ReadTaxonomies(product, "categories"),
            ReadTaxonomies(product, "filters"),
            ReadString(product, "image"),
            ReadImages(product),
            ReadVariants(product),
            ReadLongs(product, "relates"),
            ReadString(product, "meta_title"),
            ReadString(product, "meta_description"),
            ReadString(product, "meta_keywords"),
            ReadString(product, "meta_robots"),
            ReadString(product, "redirect"),
            ReadString(product, "canonical_url"),
            ReadString(product, "version"));
    }

    public static PortalProductVariant MapVariant(string json)
    {
        using var document = JsonDocument.Parse(json);
        return MapVariant(FindObject(document.RootElement, "variant", "data"));
    }

    public static PortalProductWriteModel ToWriteModel(PortalProductDetail product)
    {
        return new PortalProductWriteModel(
            product.Title ?? string.Empty,
            product.Caption,
            product.Description,
            product.Contents,
            product.CommentingEnabled,
            product.Fields,
            product.Slug,
            product.MetaTitle,
            product.MetaDescription,
            product.MetaKeywords,
            product.MetaRobots,
            product.Redirect,
            product.CanonicalUrl,
            product.Image,
            product.Images.Select(image => image.Path).ToArray(),
            product.Categories.Select(category => ParseId(category.Id, "categories")).ToArray(),
            product.Filters.Select(filter => ParseId(filter.Id, "filters")).ToArray(),
            product.Relates,
            product.Attributes,
            product.Variants,
            product.Statuses,
            Published: null);
    }

    public static PortalProductCreateResult MapCreateResult(string json)
    {
        using var document = JsonDocument.Parse(json);
        return new PortalProductCreateResult(ReadRequiredString(document.RootElement, "id"));
    }

    public static PortalProductUpdateResult MapUpdateResult(string json)
    {
        using var document = JsonDocument.Parse(json);
        return new PortalProductUpdateResult(ReadString(document.RootElement, "version"));
    }

    public static IReadOnlyList<PortalCategoryNode> MapCategories(string json)
    {
        using var document = JsonDocument.Parse(json);
        var pages = FindArray(document.RootElement, "pages", "items", "data");
        return pages.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.Object)
            .Select(MapCategory)
            .ToArray();
    }

    public static IReadOnlyList<PortalFilterGroup> MapFilters(string json)
    {
        using var document = JsonDocument.Parse(json);
        var groups = FindArray(document.RootElement, "filters", "items", "data");
        return groups.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.Object)
            .Select(group => new PortalFilterGroup(
                ReadRequiredString(group, "id"),
                ReadString(group, "title"),
                ReadInt(group, "weight"),
                ReadTaxonomies(group, "subset")))
            .ToArray();
    }

    private static PortalCategoryNode MapCategory(JsonElement page)
    {
        var children = page.TryGetProperty("subset", out var subset) && subset.ValueKind == JsonValueKind.Array
            ? subset.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.Object).Select(MapCategory).ToArray()
            : [];
        return new PortalCategoryNode(
            ReadRequiredString(page, "id"),
            ReadString(page, "title"),
            ReadString(page, "type"),
            children);
    }

    private static PortalProductSummary MapSummary(JsonElement product)
    {
        return new PortalProductSummary(
            ReadRequiredString(product, "id"),
            ReadString(product, "title"),
            ReadString(product, "slug"),
            ReadString(product, "url"),
            ReadDecimal(product, "price"),
            ReadTimestamp(product, "created", "created_at"),
            ReadStrings(product, "statuses", "status"));
    }

    private static IReadOnlyList<PortalNameValue> ReadNameValues(JsonElement product, string propertyName)
    {
        if (!product.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.Object)
            .Select(value => new PortalNameValue(ReadRequiredString(value, "name"), ReadString(value, "value")))
            .ToArray();
    }

    private static IReadOnlyList<PortalAttribute> ReadAttributes(JsonElement product)
    {
        if (!product.TryGetProperty("attributes", out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.Object)
            .Select(value => new PortalAttribute(
                ReadRequiredString(value, "name"),
                ReadStrings(value, "value", "values")))
            .ToArray();
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
            .Select(value => new PortalProductImage(ReadRequiredString(value, "path"), ReadString(value, "title")))
            .ToArray();
    }

    private static IReadOnlyList<PortalProductVariant> ReadVariants(JsonElement product)
    {
        if (!product.TryGetProperty("variants", out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.Object).Select(MapVariant).ToArray();
    }

    private static PortalProductVariant MapVariant(JsonElement value)
    {
        return new PortalProductVariant(
            ReadString(value, "id"),
            ReadString(value, "product_id"),
            ReadString(value, "title"),
            ReadDecimal(value, "price"),
            ReadDecimal(value, "compare_price"),
            ReadDecimal(value, "tax"),
            ReadDecimal(value, "shipping"),
            ReadDecimal(value, "weight"),
            ReadDecimal(value, "length"),
            ReadDecimal(value, "width"),
            ReadDecimal(value, "height"),
            ReadInt(value, "stock"),
            ReadInt(value, "minimum"),
            ReadInt(value, "maximum"),
            ReadString(value, "sku"),
            ReadString(value, "image"),
            ReadString(value, "type"),
            ReadStrings(value, "statuses", "status"),
            ReadStrings(value, "files"));
    }

    private static DateTime? ReadTimestamp(JsonElement product, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!product.TryGetProperty(propertyName, out var date))
            {
                continue;
            }

            var timestamp = date.ValueKind == JsonValueKind.Object
                ? ReadLong(date, "timestamp")
                : ReadScalarLong(date);
            if (timestamp is not null)
            {
                return DateTimeOffset.FromUnixTimeSeconds(timestamp.Value).UtcDateTime;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> ReadStrings(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            return values.EnumerateArray().Select(ReadScalarString).Where(value => value is not null).Cast<string>().ToArray();
        }

        return [];
    }

    private static IReadOnlyList<long> ReadLongs(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values.EnumerateArray().Select(ReadScalarLong).Where(value => value.HasValue).Select(value => value!.Value).ToArray();
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

        throw new JsonException("پاسخ پرتال فاقد آرایهٔ مورد انتظار است.");
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

        throw new JsonException("پاسخ جزئیات پرتال فاقد شیء مورد انتظار است.");
    }

    private static long ParseId(string value, string field)
    {
        return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : throw new JsonException($"شناسهٔ نامعتبر در {field} دریافت شد.");
    }

    private static string ReadRequiredString(JsonElement element, string propertyName) =>
        ReadString(element, propertyName) ?? throw new JsonException($"فیلد الزامی {propertyName} در پاسخ پرتال وجود ندارد.");

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) ? ReadScalarString(value) : null;

    private static string? ReadScalarString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        _ => null,
    };

    private static bool ReadBool(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.True;

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

    private static long? ReadLong(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) ? ReadScalarLong(value) : null;

    private static long? ReadScalarLong(JsonElement value)
    {
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
