using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Approvals;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Shared.Kernel.Approvals;

namespace NafasLand.Admin.Modules.Catalog.Tests.Approvals;

/// <summary>Acceptance criterion 5: deleting a product's last variant is rejected.</summary>
public sealed class DeleteVariantApprovalExecutorTests
{
    private static PortalProductVariant Variant(string id) => new(
        id, "101", $"واریانت {id}", 1000, null, null, null, null, null, null, null, 5, null, null, $"SKU-{id}", null, "commodity", [], []);

    [Fact]
    public async Task حذف_واریانت_وقتی_بیش_از_یکی_مانده_موفق_است()
    {
        var portal = new FakePortalProductClient
        {
            DetailResult = FakePortalProductClient.CreateDetail() with { Variants = [Variant("v1"), Variant("v2")] },
        };
        var executor = new DeleteVariantApprovalExecutor(portal, Options.Create(new PortalOptions { TestProductId = "101" }));

        var result = await executor.ExecuteAsync("{\"productId\":\"101\",\"variantId\":\"v1\"}", Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var remaining = Assert.Single(portal.LastUpdatedProduct!.Variants);
        Assert.Equal("v2", remaining.Id);
    }

    [Fact]
    public async Task حذف_آخرین_واریانت_رد_می‌شود_و_پرتال_را_صدا_نمی‌زند()
    {
        var portal = new FakePortalProductClient
        {
            DetailResult = FakePortalProductClient.CreateDetail() with { Variants = [Variant("v1")] },
        };
        var executor = new DeleteVariantApprovalExecutor(portal, Options.Create(new PortalOptions { TestProductId = "101" }));

        var result = await executor.ExecuteAsync("{\"productId\":\"101\",\"variantId\":\"v1\"}", Context(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, portal.UpdateProductCallCount);
    }

    [Fact]
    public async Task اجرا_روی_محصول_غیرتستی_رد_می‌شود()
    {
        var portal = new FakePortalProductClient
        {
            DetailResult = FakePortalProductClient.CreateDetail() with { Id = "999", Variants = [Variant("v1"), Variant("v2")] },
        };
        var executor = new DeleteVariantApprovalExecutor(portal, Options.Create(new PortalOptions { TestProductId = "101" }));

        var result = await executor.ExecuteAsync("{\"productId\":\"999\",\"variantId\":\"v1\"}", Context(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, portal.UpdateProductCallCount);
    }

    private static ApprovalContext Context() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "correlation-id");
}
