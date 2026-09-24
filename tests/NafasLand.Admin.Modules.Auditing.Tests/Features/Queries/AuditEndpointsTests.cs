using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Auditing.Tests.Features.Queries;

public sealed class AuditEndpointsTests
{
    private const string ReadAll = "audit.read.all";
    private const string Export = "audit.export";

    private static readonly DateTime Now = DateTime.UtcNow;

    [Fact]
    public async Task بدون_audit_read_all_همهٔ_اندپوینت‌های_خواندن_403_می‌دهند()
    {
        await using var host = await AuditEndpointHost.StartAsync();
        var log = AuditEndpointHost.Log("ProductUpdated", Now, Guid.NewGuid(), "Product", "101");
        await host.SeedAsync(log);
        host.ActAs(Guid.NewGuid(), Export, "catalog.products.read");

        string[] paths =
        [
            "/api/v1/audit/logs",
            $"/api/v1/audit/logs/{log.Id}",
            $"/api/v1/audit/users/{Guid.NewGuid()}",
            "/api/v1/audit/products/101",
            "/api/v1/audit/actors",
        ];

        foreach (var path in paths)
        {
            var response = await host.Client.GetAsync(path);
            Assert.True(response.StatusCode == HttpStatusCode.Forbidden, $"{path} → {response.StatusCode}");
        }
    }

    [Fact]
    public async Task بدون_audit_export_خروجی_و_وضعیت_و_دانلود_403_می‌دهند_و_تلاش_Denied_ثبت_می‌شود()
    {
        await using var host = await AuditEndpointHost.StartAsync();
        var userId = Guid.NewGuid();
        host.ActAs(userId, ReadAll);

        var export = await host.Client.PostAsJsonAsync("/api/v1/audit/export", new { format = "csv" });
        var status = await host.Client.GetAsync($"/api/v1/audit/export/{Guid.NewGuid()}/status");
        var download = await host.Client.GetAsync($"/api/v1/audit/export/{Guid.NewGuid()}/download");

        Assert.Equal(HttpStatusCode.Forbidden, export.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, download.StatusCode);

        var denied = Assert.Single(await host.ReadLogsAsync());
        Assert.Equal("AuditExported", denied.Action);
        Assert.Equal(AuditOutcome.Denied, denied.Outcome);
        Assert.Equal(userId, denied.ActorUserId);
    }

    [Fact]
    public async Task خروجی_فقط_فیلترهای_فعال_را_می‌فرستد_و_AuditExported_ثبت_می‌کند()
    {
        var actor = Guid.NewGuid();
        await using var host = await AuditEndpointHost.StartAsync(new Dictionary<Guid, string> { [actor] = "ali" });
        await host.SeedAsync(
            AuditEndpointHost.Log("ProductUpdated", Now.AddMinutes(-3), actor, "Product", "101", AuditOutcome.Denied),
            AuditEndpointHost.Log("ProductUpdated", Now.AddMinutes(-2), actor, "Product", "102", AuditOutcome.Success));
        host.ActAs(Guid.NewGuid(), ReadAll, Export);

        // Outcome by name, exactly as the list endpoint's query string takes it.
        var response = await host.Client.PostAsJsonAsync("/api/v1/audit/export", new { format = "csv", outcome = "Denied", action = "ProductUpdated" });
        var csv = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(",101,", csv);
        Assert.DoesNotContain(",102,", csv);
        Assert.Contains(",ali,", csv);

        var exported = Assert.Single(await host.ReadLogsAsync(), log => log.Action == "AuditExported");
        Assert.Equal(AuditOutcome.Success, exported.Outcome);
        Assert.Contains("\"matchingCount\":1", exported.AfterJson);
    }

    [Fact]
    public async Task خروجی_با_outcome_نامعتبر_400_می‌دهد_نه_خروجی_بدون_فیلتر()
    {
        await using var host = await AuditEndpointHost.StartAsync();
        host.ActAs(Guid.NewGuid(), ReadAll, Export);

        var response = await host.Client.PostAsJsonAsync("/api/v1/audit/export", new { format = "csv", outcome = "2" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(await host.ReadLogsAsync(), log => log.Action == "AuditExported");
    }

    [Fact]
    public async Task لاگ_سراسری_نام_عامل_را_می‌آورد_و_کاربر_ناشناس_و_سیستم_را_از_هم_جدا_می‌کند()
    {
        var known = Guid.NewGuid();
        var unknown = Guid.NewGuid();
        await using var host = await AuditEndpointHost.StartAsync(new Dictionary<Guid, string> { [known] = "ali" });
        await host.SeedAsync(
            AuditEndpointHost.Log("ProductUpdated", Now.AddMinutes(-3), known, "Product", "101"),
            AuditEndpointHost.Log("ProductUpdated", Now.AddMinutes(-2), unknown, "Product", "101"),
            AuditEndpointHost.Log("AuditPurged", Now.AddMinutes(-1)));
        host.ActAs(Guid.NewGuid(), ReadAll);

        var items = (await GetJsonAsync(host, "/api/v1/audit/logs")).GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(3, items.Count);
        var system = items[0];
        Assert.Equal(JsonValueKind.Null, system.GetProperty("actorUserId").ValueKind);
        Assert.Equal(JsonValueKind.Null, system.GetProperty("actorUsername").ValueKind);
        Assert.Equal(unknown.ToString(), items[1].GetProperty("actorUserId").GetString());
        Assert.Equal(JsonValueKind.Null, items[1].GetProperty("actorUsername").ValueKind);
        Assert.Equal("ali", items[2].GetProperty("actorUsername").GetString());

        var actors = (await GetJsonAsync(host, "/api/v1/audit/actors")).EnumerateArray().ToList();
        Assert.Equal(["ali", null], actors.Select(actor => actor.GetProperty("username").GetString()));
    }

    [Fact]
    public async Task فعالیت_کاربر_countsByAction_را_فقط_در_بازهٔ_تاریخ_می‌شمارد()
    {
        var actor = Guid.NewGuid();
        await using var host = await AuditEndpointHost.StartAsync(new Dictionary<Guid, string> { [actor] = "ali" });
        await host.SeedAsync(
            AuditEndpointHost.Log("ProductCreated", Now.AddDays(-10), actor, "Product", "1"),
            AuditEndpointHost.Log("ProductUpdated", Now.AddDays(-2), actor, "Product", "1"),
            AuditEndpointHost.Log("ProductUpdated", Now.AddDays(-1), actor, "Product", "1"),
            AuditEndpointHost.Log("ApprovalRequested", Now.AddDays(-1), actor, "Product", "1", AuditOutcome.Denied),
            AuditEndpointHost.Log("ProductUpdated", Now.AddDays(-1), Guid.NewGuid(), "Product", "1"));
        host.ActAs(Guid.NewGuid(), ReadAll);

        var from = Uri.EscapeDataString(new DateTimeOffset(Now.AddDays(-5)).ToString("O"));
        var body = await GetJsonAsync(host, $"/api/v1/audit/users/{actor}?from={from}");

        Assert.Equal("ali", body.GetProperty("username").GetString());
        var byAction = body.GetProperty("countsByAction").EnumerateArray()
            .ToDictionary(
                item => $"{item.GetProperty("action").GetString()}/{item.GetProperty("outcome").GetString()}",
                item => item.GetProperty("count").GetInt32());
        Assert.Equal(new Dictionary<string, int> { ["ProductUpdated/Success"] = 2, ["ApprovalRequested/Denied"] = 1 }, byAction);
        var byOutcome = body.GetProperty("countsByOutcome").EnumerateArray()
            .ToDictionary(item => item.GetProperty("outcome").GetString()!, item => item.GetProperty("count").GetInt32());
        Assert.Equal(new Dictionary<string, int> { ["Success"] = 2, ["Denied"] = 1 }, byOutcome);
        Assert.Equal(3, body.GetProperty("timeline").GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task تاریخچهٔ_محصول_تغییر_واریانت_همان_محصول_را_دارد_و_واریانت_محصول_دیگر_را_ندارد()
    {
        await using var host = await AuditEndpointHost.StartAsync();
        await host.SeedProductRefAsync("101", "کرم مرطوب‌کننده");
        await host.SeedAsync(
            AuditEndpointHost.Log("ProductUpdated", Now.AddMinutes(-5), Guid.NewGuid(), "Product", "101"),
            AuditEndpointHost.Log("VariantPriceInventoryUpdated", Now.AddMinutes(-4), Guid.NewGuid(), "ProductVariant", "501", parentEntityType: "Product", parentEntityId: "101"),
            AuditEndpointHost.Log("VariantPriceInventoryUpdated", Now.AddMinutes(-3), Guid.NewGuid(), "ProductVariant", "601", parentEntityType: "Product", parentEntityId: "202"),
            AuditEndpointHost.Log("ProductUpdated", Now.AddMinutes(-2), Guid.NewGuid(), "Product", "202"));
        host.ActAs(Guid.NewGuid(), ReadAll);

        var body = await GetJsonAsync(host, "/api/v1/audit/products/101");

        Assert.Equal("کرم مرطوب‌کننده", body.GetProperty("lastKnownTitle").GetString());
        var entities = body.GetProperty("items").EnumerateArray()
            .Select(item => $"{item.GetProperty("entityType").GetString()}:{item.GetProperty("entityId").GetString()}")
            .ToList();
        Assert.Equal(["ProductVariant:501", "Product:101"], entities);
    }

    [Fact]
    public async Task تاریخچهٔ_محصول_رکورد_قدیمی_واریانت_بدون_والد_را_با_شناسهٔ_واریانت_ارسالی_پیدا_می‌کند()
    {
        await using var host = await AuditEndpointHost.StartAsync();
        await host.SeedAsync(
            AuditEndpointHost.Log("VariantPriceInventoryUpdated", Now.AddMinutes(-4), Guid.NewGuid(), "ProductVariant", "501"),
            AuditEndpointHost.Log("ApprovalRequested", Now.AddMinutes(-3), Guid.NewGuid(), "Variant", "501"),
            AuditEndpointHost.Log("VariantPriceInventoryUpdated", Now.AddMinutes(-2), Guid.NewGuid(), "ProductVariant", "601"));
        host.ActAs(Guid.NewGuid(), ReadAll);

        var without = await GetJsonAsync(host, "/api/v1/audit/products/101");
        var with = await GetJsonAsync(host, "/api/v1/audit/products/101?variantId=501");

        Assert.Equal(0, without.GetProperty("items").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, without.GetProperty("lastKnownTitle").ValueKind);
        Assert.Equal(
            ["ApprovalRequested", "VariantPriceInventoryUpdated"],
            with.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("action").GetString()));
    }

    [Fact]
    public async Task جزئیات_رویداد_کلیدهای_حساس_را_در_هر_عمقی_پنهان_می‌کند()
    {
        await using var host = await AuditEndpointHost.StartAsync();
        var log = AuditEndpointHost.Log(
            "UserCreated",
            Now,
            Guid.NewGuid(),
            "User",
            "u1",
            afterJson: """{"Username":"ali","PasswordHash":"argon2$abc","Nested":{"TemporaryPassword":"P@ss"},"Items":[{"accessToken":"t"}]}""");
        await host.SeedAsync(log);
        host.ActAs(Guid.NewGuid(), ReadAll);

        var body = await GetJsonAsync(host, $"/api/v1/audit/logs/{log.Id}");
        var raw = body.GetProperty("after").GetRawText();

        Assert.Equal("ali", body.GetProperty("after").GetProperty("Username").GetString());
        Assert.DoesNotContain("argon2", raw);
        Assert.DoesNotContain("P@ss", raw);
        Assert.DoesNotContain("\"t\"", raw);
    }

    private static async Task<JsonElement> GetJsonAsync(AuditEndpointHost host, string path)
    {
        var response = await host.Client.GetAsync(path);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{path} → {response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }
}
