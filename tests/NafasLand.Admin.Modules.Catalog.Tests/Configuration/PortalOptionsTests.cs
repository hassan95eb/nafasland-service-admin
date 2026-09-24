using System.ComponentModel.DataAnnotations;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Shared.Infrastructure.Portal;

namespace NafasLand.Admin.Modules.Catalog.Tests.Configuration;

public sealed class PortalOptionsTests
{
    [Fact]
    public void نبودن_توکن_اعتبارسنجی_کانفیگ_را_رد_می‌کند()
    {
        var options = new PortalConnectionOptions
        {
            BaseUrl = "https://portal.invalid/site/api/v1/manage",
        };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(PortalConnectionOptions.BearerToken)));
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

    [Fact]
    public void محافظ_محصول_تستی_به_صورت_پیش‌فرض_فعال_است()
    {
        var options = new PortalOptions { TestProductId = "101" };

        Assert.True(options.RestrictWritesToTestProduct);
        Assert.True(options.IsWriteAllowed("101"));
        Assert.False(options.IsWriteAllowed("202"));
    }

    [Fact]
    public void با_خاموش_کردن_محافظ_نوشتن_روی_هر_محصولی_مجاز_است()
    {
        var options = new PortalOptions { TestProductId = "101", RestrictWritesToTestProduct = false };

        Assert.True(options.IsWriteAllowed("202"));
    }
}
