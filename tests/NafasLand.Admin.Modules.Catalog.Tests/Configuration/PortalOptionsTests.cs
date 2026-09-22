using System.ComponentModel.DataAnnotations;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;

namespace NafasLand.Admin.Modules.Catalog.Tests.Configuration;

public sealed class PortalOptionsTests
{
    [Fact]
    public void نبودن_توکن_اعتبارسنجی_کانفیگ_را_رد_می‌کند()
    {
        var options = new PortalOptions
        {
            BaseUrl = "https://portal.invalid/site/api/v1/manage",
            TestProductId = "101",
        };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(PortalOptions.BearerToken)));
    }

    [Fact]
    public void permission_خواندن_برای_سوپرادمین_رزرو_نشده_است()
    {
        var permission = Assert.Single(new CatalogModule().Permissions, value => value.Key == "catalog.products.read");

        Assert.Equal("catalog.products.read", permission.Key);
        Assert.False(permission.IsSuperAdminOnly);
    }

    [Fact]
    public void ایجاد_محصول_به_صورت_پیش‌فرض_غیرفعال_است()
    {
        Assert.False(new PortalOptions().AllowProductCreation);
    }
}
