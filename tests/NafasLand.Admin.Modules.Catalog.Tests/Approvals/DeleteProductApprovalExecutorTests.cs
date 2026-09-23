using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Approvals;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Tests.Approvals;

public sealed class DeleteProductApprovalExecutorTests
{
    [Fact]
    public async Task اجرای_موفق_محصول_تستی_را_از_پرتال_حذف_می‌کند()
    {
        var portal = new FakePortalProductClient();
        var executor = new DeleteProductApprovalExecutor(portal, Options.Create(new PortalOptions { TestProductId = "101" }));

        var result = await executor.ExecuteAsync("{\"productId\":\"101\"}", Context(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, portal.DeleteProductCallCount);
        Assert.Equal("101", portal.LastDeletedProductId);
    }

    [Fact]
    public async Task اجرا_روی_محصول_غیرتستی_رد_می‌شود_و_پرتال_را_صدا_نمی‌زند()
    {
        var portal = new FakePortalProductClient();
        var executor = new DeleteProductApprovalExecutor(portal, Options.Create(new PortalOptions { TestProductId = "101" }));

        var result = await executor.ExecuteAsync("{\"productId\":\"999\"}", Context(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, portal.DeleteProductCallCount);
    }

    [Fact]
    public async Task پیش‌نمایش_روی_محصول_غیرتستی_رد_می‌شود()
    {
        var portal = new FakePortalProductClient();
        var executor = new DeleteProductApprovalExecutor(portal, Options.Create(new PortalOptions { TestProductId = "101" }));

        await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            executor.PreviewAsync("{\"productId\":\"999\"}", CancellationToken.None));
    }

    [Fact]
    public async Task پیش‌نمایش_عنوان_محصول_را_زنده_می‌خواند()
    {
        var portal = new FakePortalProductClient();
        var executor = new DeleteProductApprovalExecutor(portal, Options.Create(new PortalOptions { TestProductId = "101" }));

        var preview = await executor.PreviewAsync("{\"productId\":\"101\"}", CancellationToken.None);

        Assert.Equal("محصول نمونه", preview.EntityTitle);
        Assert.Equal(1, portal.DetailCallCount);
    }

    private static ApprovalContext Context() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "correlation-id");
}
