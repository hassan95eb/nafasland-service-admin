using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Approvals;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Shared.Kernel.Approvals;

namespace NafasLand.Admin.Modules.Catalog.Tests.Approvals;

public sealed class PublishProductApprovalExecutorTests
{
    [Fact]
    public async Task انتشار_pending_را_به_approved_تبدیل_می‌کند()
    {
        var portal = new FakePortalProductClient();
        var executor = new PublishProductApprovalExecutor(portal, Options.Create(new PortalOptions { TestProductId = "101" }));

        var result = await executor.ExecuteAsync("{\"productId\":\"101\",\"publish\":true}", Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("pending", portal.LastUpdatedProduct!.Status);
        Assert.Contains("approved", portal.LastUpdatedProduct.Status);
    }

    [Fact]
    public async Task لغو_انتشار_approved_را_به_pending_تبدیل_می‌کند()
    {
        var portal = new FakePortalProductClient
        {
            DetailResult = FakePortalProductClient.CreateDetail() with { Statuses = ["approved", "available"] },
        };
        var executor = new PublishProductApprovalExecutor(portal, Options.Create(new PortalOptions { TestProductId = "101" }));

        var result = await executor.ExecuteAsync("{\"productId\":\"101\",\"publish\":false}", Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("approved", portal.LastUpdatedProduct!.Status);
        Assert.Contains("pending", portal.LastUpdatedProduct.Status);
        // Read-merge-write: fields other than Status are untouched.
        Assert.Contains("available", portal.LastUpdatedProduct.Status);
    }

    [Fact]
    public async Task اجرا_روی_محصول_غیرتستی_رد_می‌شود()
    {
        var portal = new FakePortalProductClient();
        var executor = new PublishProductApprovalExecutor(portal, Options.Create(new PortalOptions { TestProductId = "101" }));

        var result = await executor.ExecuteAsync("{\"productId\":\"999\",\"publish\":true}", Context(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, portal.UpdateProductCallCount);
    }

    private static ApprovalContext Context() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "correlation-id");
}
