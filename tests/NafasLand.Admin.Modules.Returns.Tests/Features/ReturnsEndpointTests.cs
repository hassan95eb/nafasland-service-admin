using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Returns.Tests.Features;

public sealed class ReturnsEndpointTests
{
    private const string Request = "returns.request";
    private const string Review = "returns.review";
    private const string ReadAll = "returns.read.all";
    private const string ApprovalsReview = "approvals.review";
    private const string ApprovalsReadAll = "approvals.read.all";

    private static readonly Guid Admin = Guid.NewGuid();
    private static readonly Guid OtherAdmin = Guid.NewGuid();
    private static readonly Guid SuperAdmin = Guid.NewGuid();

    [Fact]
    public async Task بدون_returns_request_پیش‌نمایش_سفارش_و_ثبت_درخواست_403_و_Denied_در_AuditLog_است()
    {
        var portal = new FakePortalOrderClient().With(OrderFixtures.Paid());
        await using var host = await ReturnsEndpointHost.StartAsync(portal);
        host.ActAs(Admin);

        var preview = await host.Client.GetAsync($"/api/v1/returns/orders/{OrderFixtures.OrderId}");
        var filing = await FileAsync(host);

        Assert.Equal(HttpStatusCode.Forbidden, preview.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, filing.StatusCode);
        Assert.Equal(0, portal.CallCount);
        Assert.Contains(host.AuditLog.Entries, entry =>
            entry.Outcome == AuditOutcome.Denied && entry.Action == "ApprovalRequested" && entry.ActorUserId == Admin);
    }

    [Fact]
    public async Task پیش‌نمایش_سفارش_فقط_فیلدهای_مجاز_را_برمی‌گرداند()
    {
        await using var host = await ReturnsEndpointHost.StartAsync(new FakePortalOrderClient().With(OrderFixtures.Paid()));
        host.ActAs(Admin, Request);

        var response = await host.Client.GetAsync($"/api/v1/returns/orders/{OrderFixtures.OrderId}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1595000m, json.RootElement.GetProperty("total").GetDecimal());
        Assert.Equal("مشتری آزمایشی", json.RootElement.GetProperty("customerName").GetString());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("ineligibilityReason").ValueKind);
        Assert.DoesNotContain("09000000000", body, StringComparison.Ordinal);
        Assert.DoesNotContain("نشانی ساختگی", body, StringComparison.Ordinal);
        Assert.DoesNotContain("203.0.113.10", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task سفارش_ناموجود_404_با_پیام_فارسی_و_correlationId_است()
    {
        await using var host = await ReturnsEndpointHost.StartAsync(new FakePortalOrderClient());
        host.ActAs(Admin, Request);

        var response = await host.Client.GetAsync("/api/v1/returns/orders/42");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("سفارشی با این شناسه پیدا نشد", json.RootElement.GetProperty("detail").GetString());
        Assert.True(json.RootElement.TryGetProperty("correlationId", out _));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("1.5")]
    public async Task شمارهٔ_سفارش_غیر_عدد_صحیح_مثبت_400_است(string orderId)
    {
        var portal = new FakePortalOrderClient();
        await using var host = await ReturnsEndpointHost.StartAsync(portal);
        host.ActAs(Admin, Request);

        var response = await host.Client.GetAsync($"/api/v1/returns/orders/{orderId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, portal.CallCount);
    }

    [Fact]
    public async Task خطای_پرتال_503_بدون_پیام_خام_و_با_correlationId_است()
    {
        await using var host = await ReturnsEndpointHost.StartAsync(FakePortalOrderClient.Unavailable());
        host.ActAs(Admin, Request);

        var response = await host.Client.GetAsync("/api/v1/returns/orders/42");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(json.RootElement.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task سفارش_لغوشده_در_پیش‌نمایش_دلیل_عدم_صلاحیت_و_در_ثبت_422_می‌دهد()
    {
        await using var host = await ReturnsEndpointHost.StartAsync(
            new FakePortalOrderClient().With(OrderFixtures.WithStatuses("paid", "canceled")));
        host.ActAs(Admin, Request);

        var preview = await host.Client.GetFromJsonAsync<JsonElement>($"/api/v1/returns/orders/{OrderFixtures.OrderId}");
        var filing = await FileAsync(host);

        Assert.Contains("لغو", preview.GetProperty("ineligibilityReason").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, filing.StatusCode);
    }

    [Fact]
    public async Task ثبت_سپس_تأیید_رکورد_می‌سازد_و_ثبت_دوم_409_است()
    {
        var usernames = new Dictionary<Guid, string> { [Admin] = "admin1", [SuperAdmin] = "root" };
        await using var host = await ReturnsEndpointHost.StartAsync(new FakePortalOrderClient().With(OrderFixtures.Paid()), usernames);

        host.ActAs(Admin, Request);
        var approvalId = await FileAndReadIdAsync(host);
        Assert.Empty(await host.ReadRecordsAsync());

        host.ActAs(SuperAdmin, ApprovalsReview, ApprovalsReadAll, Review, ReadAll);
        var detail = await host.Client.GetFromJsonAsync<JsonElement>($"/api/v1/approvals/{approvalId}");
        Assert.Equal("سفارش 900000001", detail.GetProperty("preview").GetProperty("entityTitle").GetString());

        var approve = await host.Client.PostAsJsonAsync($"/api/v1/approvals/{approvalId}/approval", new { note = (string?)null });
        var decision = await approve.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        Assert.Equal("Executed", decision.GetProperty("status").GetString());

        var record = Assert.Single(await host.ReadRecordsAsync());
        Assert.Equal((Admin, SuperAdmin), (record.RegisteredByUserId, record.ApprovedByUserId));
        Assert.Equal(approvalId, record.ApprovalRequestId);

        var list = await host.Client.GetFromJsonAsync<JsonElement>("/api/v1/returns");
        var item = Assert.Single(list.GetProperty("items").EnumerateArray());
        Assert.Equal("admin1", item.GetProperty("registeredByUsername").GetString());
        Assert.Equal("root", item.GetProperty("approvedByUsername").GetString());

        host.ActAs(Admin, Request);
        var duplicate = await FileAsync(host);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task رد_درخواست_هیچ_رکوردی_نمی‌سازد()
    {
        await using var host = await ReturnsEndpointHost.StartAsync(new FakePortalOrderClient().With(OrderFixtures.Paid()));
        host.ActAs(Admin, Request);
        var approvalId = await FileAndReadIdAsync(host);

        host.ActAs(SuperAdmin, ApprovalsReview, ApprovalsReadAll, Review, ReadAll);
        var reject = await host.Client.PostAsJsonAsync($"/api/v1/approvals/{approvalId}/rejection", new { note = "سفارش مرجوعی ندارد" });

        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);
        Assert.Empty(await host.ReadRecordsAsync());
    }

    [Fact]
    public async Task ادمین_فقط_مرجوعی‌های_خودش_را_می‌بیند_و_جزئیات_دیگری_برایش_404_است()
    {
        var portal = new FakePortalOrderClient().With(OrderFixtures.Paid(1001)).With(OrderFixtures.Paid(1002));
        await using var host = await ReturnsEndpointHost.StartAsync(portal);

        host.ActAs(Admin, Request);
        var own = await FileAndReadIdAsync(host, 1001);
        host.ActAs(OtherAdmin, Request);
        var others = await FileAndReadIdAsync(host, 1002);

        host.ActAs(SuperAdmin, ApprovalsReview, ApprovalsReadAll, Review, ReadAll);
        foreach (var id in new[] { own, others })
        {
            var approve = await host.Client.PostAsJsonAsync($"/api/v1/approvals/{id}/approval", new { note = (string?)null });
            Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        }

        var all = await host.Client.GetFromJsonAsync<JsonElement>("/api/v1/returns");
        Assert.Equal(2, all.GetProperty("items").GetArrayLength());

        var records = await host.ReadRecordsAsync();
        var ownRecord = records.Single(record => record.OrderId == 1001);
        var otherRecord = records.Single(record => record.OrderId == 1002);

        host.ActAs(Admin, Request);
        var mine = await host.Client.GetFromJsonAsync<JsonElement>("/api/v1/returns");
        var item = Assert.Single(mine.GetProperty("items").EnumerateArray());
        Assert.Equal(1001, item.GetProperty("orderId").GetInt64());

        var ownDetail = await host.Client.GetAsync($"/api/v1/returns/{ownRecord.Id}");
        var otherDetail = await host.Client.GetAsync($"/api/v1/returns/{otherRecord.Id}");
        Assert.Equal(HttpStatusCode.OK, ownDetail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherDetail.StatusCode);
    }

    [Fact]
    public void review_و_read_all_فقط_مخصوص_سوپرادمین‌اند_و_request_قابل_انتساب_است()
    {
        var permissions = new ReturnsModule().Permissions.ToDictionary(permission => permission.Key, permission => permission.IsSuperAdminOnly);

        Assert.Equal(3, permissions.Count);
        Assert.False(permissions[Request]);
        Assert.True(permissions[Review]);
        Assert.True(permissions[ReadAll]);
    }

    private static Task<HttpResponseMessage> FileAsync(ReturnsEndpointHost host, long orderId = OrderFixtures.OrderId) =>
        host.Client.PostAsJsonAsync("/api/v1/approvals", new
        {
            requestType = "returns.register",
            targetEntityType = "Order",
            targetEntityId = orderId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            reason = "کالا آسیب دیده بود",
            payload = new { orderId, reason = "کالا آسیب دیده بود", returnDate = "2026-09-24" },
        });

    private static async Task<Guid> FileAndReadIdAsync(ReturnsEndpointHost host, long orderId = OrderFixtures.OrderId)
    {
        var response = await FileAsync(host, orderId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }
}
