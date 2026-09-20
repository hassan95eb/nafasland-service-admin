using NafasLand.Admin.Shared.Infrastructure.Auditing;

namespace NafasLand.Admin.Shared.Kernel.Tests.Auditing;

public sealed class AuditContextTests
{
    [Fact]
    public void وقتی_فقط_After_ست_شده_ChangedFields_خالی_است_و_فقط_AfterJson_ثبت_می‌شود()
    {
        var context = new AuditContext();

        context.SetAfter(new { Username = "ali", IsActive = true });

        Assert.Empty(context.ComputeChangedFields());
        Assert.Null(context.BeforeJson);
        Assert.NotNull(context.AfterJson);
    }

    [Fact]
    public void وقتی_فقط_Before_ست_شده_ChangedFields_خالی_است()
    {
        var context = new AuditContext();

        context.SetBefore(new { Username = "ali", IsActive = true });

        Assert.Empty(context.ComputeChangedFields());
    }

    [Fact]
    public void وقتی_هر_دو_Before_و_After_ست_شده‌اند_فقط_فیلدهای_تغییرکرده_برمی‌گردند()
    {
        var context = new AuditContext();

        context.SetBefore(new { Username = "ali", IsActive = true });
        context.SetAfter(new { Username = "ali", IsActive = false });

        var changed = context.ComputeChangedFields();

        Assert.Single(changed);
        Assert.Contains("IsActive", changed);
    }

    [Fact]
    public void فیلد_اضافه‌شده_در_After_هم_در_ChangedFields_می‌آید()
    {
        var context = new AuditContext();

        context.SetBefore(new { Username = "ali" });
        context.SetAfter(new { Username = "ali", MustChangePassword = true });

        var changed = context.ComputeChangedFields();

        Assert.Contains("MustChangePassword", changed);
    }

    [Fact]
    public void وقتی_هیچ‌کدام_ست_نشده_ChangedFields_خالی_است()
    {
        var context = new AuditContext();

        Assert.Empty(context.ComputeChangedFields());
    }
}
