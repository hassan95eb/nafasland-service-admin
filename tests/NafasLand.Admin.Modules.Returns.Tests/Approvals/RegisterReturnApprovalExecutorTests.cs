using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Returns.Approvals;
using NafasLand.Admin.Modules.Returns.Persistence;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Returns.Tests.Approvals;

public sealed class RegisterReturnApprovalExecutorTests
{
    private static readonly DateTime RequestedAt = new(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ReviewedAt = new(2026, 9, 25, 7, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task سفارش_پرداخت‌شده_بعد_از_تأیید_رکورد_با_عکس_فوری_می‌سازد()
    {
        await using var dbContext = ReturnsDbContextTestFactory.Create();
        var executor = CreateExecutor(new FakePortalOrderClient().With(OrderFixtures.Paid()), dbContext);
        var context = Context();

        var result = await executor.ExecuteAsync(Payload(), context, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        var record = Assert.Single(dbContext.ReturnRecords);
        Assert.Equal(context.ApprovalRequestId, record.ApprovalRequestId);
        Assert.Equal(OrderFixtures.OrderId, record.OrderId);
        Assert.Equal("مشتری آزمایشی", record.CustomerName);
        Assert.Equal(1595000m, record.Total);
        Assert.Equal(1400000m, record.Subtotal);
        Assert.Equal(OrderFixtures.CreatedAtUtc, record.OrderCreatedAtUtc);
        Assert.Equal(["fulfilled", "paid", "shipping_required"], record.OrderStatuses);
        var item = Assert.Single(record.Items);
        Assert.Equal((700000001L, 800000001L, 700000m, 2), (item.ProductId!.Value, item.VariantId!.Value, item.Price!.Value, item.Quantity));
        Assert.Equal("کالا آسیب دیده بود", record.Reason);
        Assert.Equal(new DateOnly(2026, 9, 24), record.ReturnDate);
        Assert.Equal((context.RequestedByUserId, RequestedAt), (record.RegisteredByUserId, record.RegisteredAt));
        Assert.Equal((context.ReviewedByUserId, ReviewedAt), (record.ApprovedByUserId, record.ApprovedAt));
    }

    [Fact]
    public async Task پیش‌نمایش_سفارش_پرداخت‌شده_نام_اقلام_مبلغ_علت_و_تاریخ_شمسی_را_دارد()
    {
        await using var dbContext = ReturnsDbContextTestFactory.Create();
        var executor = CreateExecutor(new FakePortalOrderClient().With(OrderFixtures.Paid()), dbContext);

        var preview = await executor.PreviewAsync(Payload(), CancellationToken.None);

        Assert.Equal("سفارش 900000001", preview.EntityTitle);
        var fields = preview.Fields.ToDictionary(field => field.Label, field => field.Value);
        Assert.Equal("مشتری آزمایشی", fields["نام مشتری"]);
        Assert.Equal("محصول آزمایشی ۵۰ میلی‌لیتر × 2", fields["اقلام"]);
        Assert.Equal("1595000", fields["مبلغ کل (تومان)"]);
        Assert.Equal("کالا آسیب دیده بود", fields["علت مرجوعی"]);
        Assert.Equal("1405/07/02", fields["تاریخ عودت"]);
    }

    [Theory]
    [InlineData("paid", "canceled")]
    [InlineData("fulfilled")]
    [InlineData]
    public async Task سفارش_لغوشده_یا_پرداخت‌نشده_در_ثبت_422_و_در_اجرا_Failure_است(params string[] statuses)
    {
        await using var dbContext = ReturnsDbContextTestFactory.Create();
        var executor = CreateExecutor(new FakePortalOrderClient().With(OrderFixtures.WithStatuses(statuses)), dbContext);

        await Assert.ThrowsAsync<BusinessRuleException>(() => executor.PreviewAsync(Payload(), CancellationToken.None));
        var result = await executor.ExecuteAsync(Payload(), Context(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(dbContext.ReturnRecords);
    }

    [Fact]
    public async Task مرجوعی_تکراری_برای_یک_سفارش_در_ثبت_409_و_در_اجرا_Failure_است()
    {
        await using var dbContext = ReturnsDbContextTestFactory.Create();
        var executor = CreateExecutor(new FakePortalOrderClient().With(OrderFixtures.Paid()), dbContext);
        Assert.True((await executor.ExecuteAsync(Payload(), Context(), CancellationToken.None)).IsSuccess);

        var conflict = await Assert.ThrowsAsync<ConflictException>(() => executor.PreviewAsync(Payload(), CancellationToken.None));
        var second = await executor.ExecuteAsync(Payload(), Context(), CancellationToken.None);

        Assert.Equal(ReturnRules.AlreadyRegistered, conflict.Message);
        Assert.False(second.IsSuccess);
        Assert.Equal(ReturnRules.AlreadyRegistered, second.Error);
        Assert.Single(dbContext.ReturnRecords);
    }

    [Fact]
    public async Task retry_بعد_از_ExecutionFailed_رکورد_دوم_نمی‌سازد()
    {
        var databaseName = Guid.NewGuid().ToString();
        var context = Context();
        var portal = FakePortalOrderClient.Unavailable();
        await using (var dbContext = ReturnsDbContextTestFactory.Create(databaseName))
        {
            var failed = await CreateExecutor(portal, dbContext).ExecuteAsync(Payload(), context, CancellationToken.None);
            Assert.False(failed.IsSuccess);
        }

        portal.Failure = null;
        portal.With(OrderFixtures.Paid());
        await using (var dbContext = ReturnsDbContextTestFactory.Create(databaseName))
        {
            Assert.True((await CreateExecutor(portal, dbContext).ExecuteAsync(Payload(), context, CancellationToken.None)).IsSuccess);
        }

        // A second retry of the same approval (e.g. the first retry saved the
        // record but its response was lost) is a no-op success.
        await using (var dbContext = ReturnsDbContextTestFactory.Create(databaseName))
        {
            var again = await CreateExecutor(portal, dbContext).ExecuteAsync(Payload(), context, CancellationToken.None);
            Assert.True(again.IsSuccess);
            Assert.Single(dbContext.ReturnRecords);
        }
    }

    [Fact]
    public void ApprovalRequestId_و_OrderId_در_دیتابیس_محدودیت_یکتایی_دارند()
    {
        using var dbContext = ReturnsDbContextTestFactory.Create();
        var entity = dbContext.Model.FindEntityType(typeof(ReturnRecord))!;

        Assert.Contains(entity.GetIndexes(), index => index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual([nameof(ReturnRecord.ApprovalRequestId)]));
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique
            && index.Properties.Select(property => property.Name).SequenceEqual([nameof(ReturnRecord.OrderId)]));
    }

    [Fact]
    public async Task خطای_پرتال_در_اجرا_به_Result_Failure_تبدیل_می‌شود_نه_استثنا()
    {
        await using var dbContext = ReturnsDbContextTestFactory.Create();
        var executor = CreateExecutor(FakePortalOrderClient.Unavailable(), dbContext);

        var result = await executor.ExecuteAsync(Payload(), Context(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(new PortalUnavailableException().Message, result.Error);
        Assert.Empty(dbContext.ReturnRecords);
    }

    [Fact]
    public async Task سفارش_ناموجود_در_ثبت_422_با_پیام_فارسی_است()
    {
        await using var dbContext = ReturnsDbContextTestFactory.Create();
        var executor = CreateExecutor(new FakePortalOrderClient(), dbContext);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => executor.PreviewAsync(Payload(), CancellationToken.None));

        Assert.Equal("سفارشی با این شناسه پیدا نشد", exception.Message);
    }

    [Theory]
    [InlineData("2026-09-26", "returnDate")]
    [InlineData(null, "returnDate")]
    public async Task تاریخ_عودت_آینده_یا_خالی_در_ثبت_400_است(string? returnDate, string field)
    {
        await using var dbContext = ReturnsDbContextTestFactory.Create();
        var executor = CreateExecutor(new FakePortalOrderClient().With(OrderFixtures.Paid()), dbContext);

        var exception = await Assert.ThrowsAsync<CommandValidationException>(() =>
            executor.PreviewAsync(Payload(returnDate: returnDate), CancellationToken.None));

        Assert.Contains(exception.Errors.Keys, key => key.Equals(field, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task تاریخ_عودت_پیش_از_ایجاد_سفارش_422_است()
    {
        await using var dbContext = ReturnsDbContextTestFactory.Create();
        var executor = CreateExecutor(new FakePortalOrderClient().With(OrderFixtures.Paid()), dbContext);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            executor.PreviewAsync(Payload(returnDate: "2026-09-23"), CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task علت_خالی_در_ثبت_400_است(string reason)
    {
        await using var dbContext = ReturnsDbContextTestFactory.Create();
        var executor = CreateExecutor(new FakePortalOrderClient().With(OrderFixtures.Paid()), dbContext);

        await Assert.ThrowsAsync<CommandValidationException>(() => executor.PreviewAsync(Payload(reason: reason), CancellationToken.None));
    }

    [Fact]
    public void مجری_دسترسی‌های_ADR054_را_اعلام_می‌کند()
    {
        using var dbContext = ReturnsDbContextTestFactory.Create();
        var executor = CreateExecutor(new FakePortalOrderClient(), dbContext);

        Assert.Equal("returns.register", executor.RequestType);
        Assert.Equal("returns.request", executor.RequestPermission);
        Assert.Equal("returns.review", executor.RequiredPermission);
    }

    internal static RegisterReturnApprovalExecutor CreateExecutor(FakePortalOrderClient portal, ReturnsDbContext dbContext) =>
        new(portal, dbContext, new RegisterReturnPayloadValidator(new FixedTimeProvider(OrderFixtures.Now)));

    private static ApprovalContext Context() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "correlation-id", RequestedAt, ReviewedAt);

    private static string Payload(long orderId = OrderFixtures.OrderId, string? reason = "کالا آسیب دیده بود", string? returnDate = "2026-09-24") =>
        JsonSerializer.Serialize(new { orderId, reason, returnDate });
}
