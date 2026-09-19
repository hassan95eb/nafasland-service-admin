using NafasLand.Admin.Modules.Identity.Security;

namespace NafasLand.Admin.Modules.Identity.Tests.Security;

/// <summary>ADR-021's rule: effective = (role permissions) + Grant − Deny, Deny always winning. The prompt's required "effective permission calculation" unit test.</summary>
public sealed class EffectivePermissionCalculatorTests
{
    [Fact]
    public void فقط_permissionهای_نقش_را_برمی‌گرداند_وقتی_Grant_و_Deny_خالی‌اند()
    {
        var effective = EffectivePermissionCalculator.Calculate(
            rolePermissionKeys: ["a.read", "b.read"],
            grantedKeys: [],
            deniedKeys: []);

        Assert.Equal(new HashSet<string> { "a.read", "b.read" }, effective);
    }

    [Fact]
    public void Grant_مستقیم_را_به_permissionهای_نقش_اضافه_می‌کند()
    {
        var effective = EffectivePermissionCalculator.Calculate(
            rolePermissionKeys: ["a.read"],
            grantedKeys: ["b.write"],
            deniedKeys: []);

        Assert.Equal(new HashSet<string> { "a.read", "b.write" }, effective);
    }

    [Fact]
    public void Deny_مستقیم_permission_نقش_را_حذف_می‌کند()
    {
        var effective = EffectivePermissionCalculator.Calculate(
            rolePermissionKeys: ["a.read", "b.read"],
            grantedKeys: [],
            deniedKeys: ["b.read"]);

        Assert.Equal(new HashSet<string> { "a.read" }, effective);
    }

    [Fact]
    public void وقتی_یک_کلید_هم_Grant_و_هم_Deny_باشد_Deny_برنده_است()
    {
        var effective = EffectivePermissionCalculator.Calculate(
            rolePermissionKeys: [],
            grantedKeys: ["a.read"],
            deniedKeys: ["a.read"]);

        Assert.Empty(effective);
    }

    [Fact]
    public void Deny_روی_permission‌ای_که_از_نقش_هم_می‌آید_همچنان_برنده_است()
    {
        var effective = EffectivePermissionCalculator.Calculate(
            rolePermissionKeys: ["a.read", "b.read"],
            grantedKeys: ["a.read"],
            deniedKeys: ["a.read"]);

        Assert.Equal(new HashSet<string> { "b.read" }, effective);
    }
}
