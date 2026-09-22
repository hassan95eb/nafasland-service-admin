using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Modules.Catalog.Features.UpdateProduct;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Tests.Features.UpdateProduct;

public sealed class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task تداخل_version_صفر_PUT_می‌فرستد()
    {
        var portal = PortalWithCompleteDetail();
        var handler = CreateHandler(portal);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.HandleAsync(Command(lastKnownVersion: "old"), CancellationToken.None));

        Assert.Equal(0, portal.UpdateProductCallCount);
    }

    [Fact]
    public async Task محصول_غیرتستی_صفر_PUT_می‌فرستد()
    {
        var portal = PortalWithCompleteDetail();
        portal.DetailResult = portal.DetailResult! with { Id = "999" };
        var handler = CreateHandler(portal);

        await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            handler.HandleAsync(Command(), CancellationToken.None));

        Assert.Equal(0, portal.UpdateProductCallCount);
    }

    [Fact]
    public async Task نبود_status_کامل_ذخیره_را_ایمن_متوقف_می‌کند()
    {
        var portal = PortalWithCompleteDetail();
        portal.DetailResult = portal.DetailResult! with { Statuses = [] };
        var handler = CreateHandler(portal);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.HandleAsync(Command(), CancellationToken.None));

        Assert.Equal(0, portal.UpdateProductCallCount);
    }

    [Fact]
    public async Task PUT_کامل_داده‌های_حفاظت‌شده_و_قیمت_و_موجودی_تازه_را_نگه_می‌دارد()
    {
        var portal = PortalWithCompleteDetail();
        var originalVariant = Assert.Single(portal.DetailResult!.Variants);
        var handler = CreateHandler(portal);

        await handler.HandleAsync(Command(), CancellationToken.None);

        var write = portal.LastUpdatedProduct!;
        Assert.Equal([11L], write.Categories);
        Assert.Equal([22L], write.Filters);
        Assert.Equal(["/uploads/products/main.webp"], write.Images);
        Assert.Equal([33L], write.Relates);
        Assert.Equal("https://canonical.test", write.CanonicalUrl);
        Assert.Equal(["approved", "available", "sale"], write.Status);
        Assert.Equal(originalVariant, Assert.Single(write.Variants));
        Assert.Equal(777m, Assert.Single(write.Variants).Price);
        Assert.Equal(9, Assert.Single(write.Variants).Stock);
        Assert.Null(write.Published);
    }

    [Fact]
    public async Task HTML_دست‌نخورده_عیناً_حفظ_و_HTML_تغییریافته_sanitize_می‌شود()
    {
        var portal = PortalWithCompleteDetail();
        const string original = "<p fr-original-style='x'>اصل&nbsp;</p>";
        portal.DetailResult = portal.DetailResult! with
        {
            Description = original,
            Contents = [new PortalNameValue("ثابت", original), new PortalNameValue("متغیر", "old")],
        };
        var handler = CreateHandler(portal);
        var command = Command() with
        {
            Description = "مقدار فرم که نباید استفاده شود",
            DescriptionDirty = false,
            Contents =
            [
                new UpdateProductContentInput("ثابت", "مقدار فرم", false),
                new UpdateProductContentInput("متغیر", "<p onclick='x()' style='text-align:justify;color:red'>نو<script>x</script></p>", true),
            ],
        };

        await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(original, portal.LastUpdatedProduct!.Description);
        Assert.Equal(original, portal.LastUpdatedProduct.Contents[0].Value);
        var changed = portal.LastUpdatedProduct.Contents[1].Value!;
        Assert.DoesNotContain("onclick", changed, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("script", changed, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("color", changed, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("text-align", changed, StringComparison.OrdinalIgnoreCase);
    }

    private static UpdateProductCommandHandler CreateHandler(FakePortalProductClient portal) => new(
        portal,
        Options.Create(new PortalOptions { TestProductId = "101" }),
        new ProductHtmlSanitizer(),
        new ProductCache(TimeProvider.System),
        new CapturingAuditContext());

    private static FakePortalProductClient PortalWithCompleteDetail()
    {
        var portal = new FakePortalProductClient();
        portal.DetailResult = FakePortalProductClient.CreateDetail() with
        {
            Categories = [new PortalTaxonomyValue("11", "دسته", null, null)],
            Filters = [new PortalTaxonomyValue("22", "فیلتر", null, null)],
            Images = [new PortalProductImage("/uploads/products/main.webp", null)],
            Relates = [33],
            CanonicalUrl = "https://canonical.test",
            Statuses = ["approved", "available", "sale"],
            Variants = [new PortalProductVariant(
                "501", "101", "primary", 777, null, 0, 0, 1, 2, 3, 4, 9, 1, 10,
                "SKU", null, "commodity", ["available"], [])],
        };
        return portal;
    }

    private static UpdateProductCommand Command(string lastKnownVersion = "1") => new(
        "101",
        lastKnownVersion,
        "عنوان جدید",
        "زیرعنوان",
        "<p>توضیح جدید</p>",
        true,
        [new UpdateProductContentInput("معرفی", "<p>محتوا</p>", true)],
        true,
        [new PortalNameValue("کشور", "ایران")],
        "عنوان-جدید",
        "متا",
        "توضیح متا",
        "کلیدواژه",
        "index,follow",
        null,
        [11],
        [22]);

    private sealed class CapturingAuditContext : IAuditContext
    {
        public void SetEntityId(string entityId) { }
        public void SetBefore(object? snapshot) { }
        public void SetAfter(object? snapshot) { }
    }
}
