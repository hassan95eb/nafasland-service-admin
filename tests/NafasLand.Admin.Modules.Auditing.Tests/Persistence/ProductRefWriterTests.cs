using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NafasLand.Admin.Modules.Auditing.Persistence;

namespace NafasLand.Admin.Modules.Auditing.Tests.Persistence;

public sealed class ProductRefWriterTests
{
    [Fact]
    public async Task اولین_بار_رکورد_می‌سازد_و_بار_بعد_عنوان_را_به‌روز_می‌کند()
    {
        await using var dbContext = AuditingDbContextTestFactory.Create();
        var writer = new ProductRefWriter(dbContext, TimeProvider.System, NullLogger<ProductRefWriter>.Instance);

        await writer.UpsertAsync("101", "عنوان اول", CancellationToken.None);
        var created = await dbContext.ProductRefs.AsNoTracking().SingleAsync();

        await writer.UpsertAsync("101", "عنوان دوم", CancellationToken.None);
        var updated = await dbContext.ProductRefs.AsNoTracking().SingleAsync();

        Assert.Equal("عنوان اول", created.LastKnownTitle);
        Assert.Equal("101", updated.ExternalProductId);
        Assert.Equal("عنوان دوم", updated.LastKnownTitle);
        Assert.True(updated.LastSyncedAt >= created.LastSyncedAt);
    }
}
