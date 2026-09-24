using System.Text.Json;
using NafasLand.Admin.Modules.Returns.Features.Queries;
using NafasLand.Admin.Modules.Returns.Infrastructure;

namespace NafasLand.Admin.Modules.Returns.Tests.Infrastructure;

public sealed class PortalOrderMapperTests
{
    /// <summary>Every personal value in the fixture other than the customer's name.</summary>
    private static readonly string[] ForbiddenValues =
    [
        "09000000000", "نشانی ساختگی", "0000000000", "203.0.113.10",
        "TEST-REFERENCE", "درگاه آزمایشی", "test-user", "استان آزمایشی", "شهر آزمایشی",
    ];

    [Fact]
    public void fixture_به_مدل_داخلی_با_مبلغ_خام_و_زمان_از_timestamp_نگاشت_می‌شود()
    {
        var order = PortalOrderMapper.Map(OrderFixtures.ReadJson())!;

        Assert.Equal(OrderFixtures.OrderId, order.OrderId);
        Assert.Equal(["fulfilled", "paid", "shipping_required"], order.Statuses);
        Assert.Equal("مشتری آزمایشی", order.CustomerName);
        Assert.Equal(1400000m, order.Subtotal);
        Assert.Equal(195000m, order.Shipping);
        Assert.Equal(0m, order.Discount);
        Assert.Equal(0m, order.Tax);
        Assert.Equal(1595000m, order.Total);
        Assert.Equal(OrderFixtures.CreatedAtUtc, order.CreatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, order.CreatedAtUtc!.Value.Kind);

        var item = Assert.Single(order.Items);
        Assert.Equal(700000001, item.ProductId);
        Assert.Equal(800000001, item.VariantId);
        Assert.Equal("محصول آزمایشی ۵۰ میلی‌لیتر", item.Title);
        Assert.Equal(700000m, item.Price);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public void هیچ_دادهٔ_شخصی_جز_نام_در_مدل_یا_پاسخ_اندپوینت_نیست()
    {
        var order = PortalOrderMapper.Map(OrderFixtures.ReadJson())!;

        var modelJson = JsonSerializer.Serialize(order);
        var responseJson = JsonSerializer.Serialize(OrderPreviewDto.FromOrder(order, null), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        foreach (var forbidden in ForbiddenValues)
        {
            Assert.DoesNotContain(forbidden, modelJson, StringComparison.Ordinal);
            Assert.DoesNotContain(forbidden, responseJson, StringComparison.Ordinal);
        }

        var responseProperties = JsonDocument.Parse(responseJson).RootElement.EnumerateObject().Select(property => property.Name).ToList();
        Assert.DoesNotContain("contact", responseProperties);
        Assert.DoesNotContain("payments", responseProperties);
        Assert.DoesNotContain("ip", responseProperties);
        Assert.DoesNotContain("user", responseProperties);
    }

    [Fact]
    public void نبود_contact_به_نام_user_و_نبود_items_به_فهرست_خالی_می‌رسد()
    {
        const string json = """
            {"success":true,"order":{"id":5,"status":["paid"],"price":10,"contact":null,
             "user":{"name":"کاربر پرتال","username":"x"},"items":null,"created":null}}
            """;

        var order = PortalOrderMapper.Map(json)!;

        Assert.Equal("کاربر پرتال", order.CustomerName);
        Assert.Empty(order.Items);
        Assert.Null(order.CreatedAtUtc);
        Assert.Null(order.Subtotal);
    }

    [Fact]
    public void success_false_یا_نبود_order_یعنی_سفارش_پیدا_نشد()
    {
        Assert.Null(PortalOrderMapper.Map("""{"success":false,"description":"not found"}"""));
        Assert.Null(PortalOrderMapper.Map("""{"success":true}"""));
    }
}
