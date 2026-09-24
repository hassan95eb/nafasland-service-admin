using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Approvals;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Tests.Approvals;

public sealed class ProductStatusApprovalExecutorTests
{
    [Theory]
    [InlineData("featured")]
    [InlineData("most")]
    public async Task وضعیت_خاموش_را_روشن_می‌کند(string statusKey)
    {
        var portal = new FakePortalProductClient();
        var executor = new ProductStatusApprovalExecutor(portal, Options.Create(new PortalOptions { TestProductId = "101" }));

        var result = await executor.ExecuteAsync($$"""{"productId":"101","statusKey":"{{statusKey}}"}""", Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(statusKey, portal.LastUpdatedProduct!.Status);
    }

    [Fact]
    public async Task وضعیت_روشن_را_خاموش_می‌کند_و_بقیه_دست‌نخورده_می‌ماند()
    {
        var portal = new FakePortalProductClient
        {
            DetailResult = FakePortalProductClient.CreateDetail() with { Statuses = ["pending", "featured"] },
        };
        var executor = new ProductStatusApprovalExecutor(portal, Options.Create(new PortalOptions { TestProductId = "101" }));

        var result = await executor.ExecuteAsync("{\"productId\":\"101\",\"statusKey\":\"featured\"}", Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("featured", portal.LastUpdatedProduct!.Status);
        Assert.Contains("pending", portal.LastUpdatedProduct.Status);
    }

    [Fact]
    public async Task کلید_وضعیت_نامعتبر_رد_می‌شود()
    {
        var portal = new FakePortalProductClient();
        var executor = new ProductStatusApprovalExecutor(portal, Options.Create(new PortalOptions { TestProductId = "101" }));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            executor.ExecuteAsync("{\"productId\":\"101\",\"statusKey\":\"sale\"}", Context(), CancellationToken.None));
    }

    private static ApprovalContext Context() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "correlation-id", DateTime.UtcNow, DateTime.UtcNow);
}
