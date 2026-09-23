using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Features.Queries;
using NafasLand.Admin.Modules.Approvals.Persistence;

namespace NafasLand.Admin.Modules.Approvals.Tests.Features.Queries;

public sealed class ApprovalRequestQueryExtensionsTests
{
    [Fact]
    public async Task صفحه‌بندی_keyset_ردیف‌های_قدیمی‌تر_از_مکان‌نما_را_برمی‌گرداند()
    {
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var now = DateTime.UtcNow;
        var requestedByUserId = Guid.NewGuid();

        var newest = ApprovalRequest.Create("catalog.product.delete", "Product", "1", "{}", null, "د", requestedByUserId, now, now.AddDays(7));
        var middle = ApprovalRequest.Create("catalog.product.delete", "Product", "2", "{}", null, "د", requestedByUserId, now.AddSeconds(-1), now.AddDays(7));
        var oldest = ApprovalRequest.Create("catalog.product.delete", "Product", "3", "{}", null, "د", requestedByUserId, now.AddSeconds(-2), now.AddDays(7));
        dbContext.ApprovalRequests.AddRange(newest, middle, oldest);
        await dbContext.SaveChangesAsync();

        var cursor = ApprovalRequestQueryExtensions.EncodeCursor(newest);
        var page = await dbContext.ApprovalRequests
            .ApplyKeysetCursor(cursor)
            .OrderByDescending(request => request.RequestedAt)
            .ThenByDescending(request => request.Id)
            .ToListAsync();

        Assert.Equal([middle.Id, oldest.Id], page.Select(request => request.Id));
    }

    [Fact]
    public void رمزگشایی_مکان‌نمای_نامعتبر_فیلتری_اعمال_نمی‌کند()
    {
        var query = new List<ApprovalRequest>().AsQueryable();
        var filtered = query.ApplyKeysetCursor("not-a-cursor");
        Assert.Same(query, filtered);
    }
}
