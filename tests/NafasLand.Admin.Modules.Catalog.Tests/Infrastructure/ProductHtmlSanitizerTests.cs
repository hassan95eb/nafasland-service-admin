using NafasLand.Admin.Modules.Catalog.Infrastructure;

namespace NafasLand.Admin.Modules.Catalog.Tests.Infrastructure;

public sealed class ProductHtmlSanitizerTests
{
    [Fact]
    public void محتوای_خطرناک_و_ویژگی‌های_Froala_حذف_و_استایل‌های_مجاز_حفظ_می‌شوند()
    {
        var sanitizer = new ProductHtmlSanitizer();
        const string dirty = "<script>alert(1)</script><iframe src='https://evil.test'></iframe>" +
            "<p onclick='evil()' fr-original-style='x' data-spread='y' " +
            "style='text-align: justify; font-weight: 700; color: red'>متن&zwnj;&nbsp;</p>" +
            "<a href='javascript:alert(1)'>لینک</a>";

        var clean = sanitizer.Sanitize(dirty)!;

        Assert.DoesNotContain("script", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("iframe", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fr-original-style", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data-spread", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("color", clean, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("text-align", clean, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("font-weight", clean, StringComparison.OrdinalIgnoreCase);
        Assert.Contains('\u200c', clean);
        Assert.Contains("&nbsp;", clean, StringComparison.OrdinalIgnoreCase);
    }
}
