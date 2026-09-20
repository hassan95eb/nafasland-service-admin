using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Modules.Catalog.Infrastructure;

namespace NafasLand.Admin.Modules.Catalog.Tests.Infrastructure;

public sealed class PortalProductMapperTests
{
    [Fact]
    public void فهرست_قیمت_تهی_تاریخ_UTC_و_وضعیت‌های_ترکیبی_را_حفظ_می‌کند()
    {
        var result = PortalProductMapper.MapList(
            ReadFixture("products-list.json"),
            new PortalProductListQuery(2, 25, "پوست", "newest"));

        Assert.Equal(5833, result.Total);
        Assert.Equal(2, result.Count);
        Assert.Null(result.Items[0].Price);
        Assert.True(result.Items[0].IsPending);
        Assert.False(result.Items[0].IsAvailable);
        Assert.Equal(DateTimeKind.Utc, result.Items[0].CreatedAtUtc!.Value.Kind);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1789282560).UtcDateTime, result.Items[0].CreatedAtUtc);
        Assert.Equal(608800m, result.Items[1].Price);
        Assert.True(result.Items[1].IsAvailable);
    }

    [Fact]
    public void جزئیات_فقط_آرایه‌های_واقعی_خواندن_را_map_می‌کند()
    {
        var product = PortalProductMapper.MapDetail(ReadFixture("product-detail.json"));

        Assert.Equal("101", product.Id);
        Assert.Equal(608800m, product.Price);
        Assert.True(product.IsPending);
        Assert.True(product.IsAvailable);
        Assert.Equal("178004128", Assert.Single(product.Categories).Id);
        Assert.Equal("179221238", Assert.Single(product.Filters).Id);
        Assert.Equal("/uploads/products/24fc99.webp", Assert.Single(product.Images).Path);
        Assert.DoesNotContain(product.Categories, category => category.Id == "999");
        Assert.DoesNotContain(product.Filters, filter => filter.Id == "998");
    }

    [Fact]
    public void جزئیات_واریانت_و_فیلدهای_حفاظت_از_SEO_را_نگه_می‌دارد()
    {
        var product = PortalProductMapper.MapDetail(ReadFixture("product-detail.json"));
        var variant = Assert.Single(product.Variants);

        Assert.Equal("501", variant.Id);
        Assert.Equal("101", variant.ProductId);
        Assert.Equal("SKU-501", variant.Sku);
        Assert.Equal(608800m, variant.Price);
        Assert.Equal(7, variant.Stock);
        Assert.Equal(["201", "202"], product.Relates);
        Assert.Equal(["پوست", "مراقبت"], product.MetaKeywords);
        Assert.Equal("https://example.test/product/101", product.CanonicalUrl);
        Assert.Equal("639250631030770000", product.Version);
    }

    private static string ReadFixture(string name)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
    }
}
