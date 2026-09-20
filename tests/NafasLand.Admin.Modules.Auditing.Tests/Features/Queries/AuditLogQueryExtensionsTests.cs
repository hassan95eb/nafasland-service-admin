using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Features.Queries;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Auditing.Tests.Features.Queries;

/// <summary>Required integration test: filter and keyset (not offset) pagination of the global log view, against a real (InMemory) DbContext.</summary>
public sealed class AuditLogQueryExtensionsTests
{
    private static readonly Guid ActorA = Guid.NewGuid();
    private static readonly Guid ActorB = Guid.NewGuid();

    private static AuditLog Seed(Guid? actorUserId, string action, AuditOutcome outcome, DateTime createdAt)
    {
        var entry = new AuditLogEntry(
            CorrelationId: Guid.NewGuid().ToString(),
            ActorUserId: actorUserId,
            OnBehalfOfUserId: null,
            ActorRoleAtTime: "Admin",
            Action: action,
            EntityType: "User",
            EntityId: "u1",
            BeforeJson: null,
            AfterJson: null,
            ChangedFields: null,
            Outcome: outcome,
            UpstreamStatus: null,
            FailureReason: null,
            IpAddress: null,
            UserAgent: null);

        return AuditLog.FromEntry(entry, createdAt);
    }

    private static async Task<AuditingDbContext> SeedFiveRowsAsync()
    {
        var dbContext = AuditingDbContextTestFactory.Create();
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        dbContext.AuditLogs.AddRange(
            Seed(ActorA, "UserCreated", AuditOutcome.Success, baseTime.AddMinutes(1)),
            Seed(ActorA, "UserCreated", AuditOutcome.Denied, baseTime.AddMinutes(2)),
            Seed(ActorB, "RolePermissionsChanged", AuditOutcome.Success, baseTime.AddMinutes(3)),
            Seed(ActorA, "UserToggled", AuditOutcome.Failed, baseTime.AddMinutes(4)),
            Seed(ActorB, "UserCreated", AuditOutcome.Success, baseTime.AddMinutes(5)));

        await dbContext.SaveChangesAsync();
        return dbContext;
    }

    [Fact]
    public async Task فیلتر_روی_ActorUserId_فقط_رکوردهای_همان_کاربر_را_برمی‌گرداند()
    {
        await using var dbContext = await SeedFiveRowsAsync();

        var filter = new AuditLogFilter(ActorA, null, null, null, null, null);
        var results = await dbContext.AuditLogs.AsNoTracking().ApplyFilter(filter).ToListAsync();

        Assert.Equal(3, results.Count);
        Assert.All(results, log => Assert.Equal(ActorA, log.ActorUserId));
    }

    [Fact]
    public async Task فیلتر_روی_Action_و_Outcome_ترکیبی_کار_می‌کند()
    {
        await using var dbContext = await SeedFiveRowsAsync();

        var filter = new AuditLogFilter(null, null, null, "UserCreated", null, AuditOutcome.Success);
        var results = await dbContext.AuditLogs.AsNoTracking().ApplyFilter(filter).ToListAsync();

        Assert.Equal(2, results.Count);
        Assert.All(results, log => Assert.Equal(AuditOutcome.Success, log.Outcome));
    }

    [Fact]
    public async Task صفحه‌بندی_keyset_روی_CreatedAt_صفحات_را_بدون_افت_یا_تکرار_رد_می‌کند()
    {
        await using var dbContext = await SeedFiveRowsAsync();

        var firstPage = await dbContext.AuditLogs.AsNoTracking()
            .OrderByDescending(log => log.CreatedAt).ThenByDescending(log => log.Id)
            .Take(2)
            .ToListAsync();
        Assert.Equal(2, firstPage.Count);

        var cursor = AuditLogQueryExtensions.EncodeCursor(firstPage[^1]);

        var secondPage = await dbContext.AuditLogs.AsNoTracking()
            .ApplyKeysetCursor(cursor)
            .OrderByDescending(log => log.CreatedAt).ThenByDescending(log => log.Id)
            .Take(2)
            .ToListAsync();

        Assert.Equal(2, secondPage.Count);
        Assert.DoesNotContain(secondPage, log => firstPage.Select(l => l.Id).Contains(log.Id));
        // Every row in the second page must be strictly older than the last row of the first page.
        Assert.All(secondPage, log => Assert.True(log.CreatedAt <= firstPage[^1].CreatedAt));
    }

    [Fact]
    public async Task نمای_محصولی_وقتی_ProductRef_وجود_ندارد_خطا_نمی‌دهد_و_LastKnownTitle_خالی_است()
    {
        await using var dbContext = AuditingDbContextTestFactory.Create();
        dbContext.AuditLogs.Add(AuditLog.FromEntry(
            new AuditLogEntry(
                "corr", null, null, "System", "ProductUpdated", "Product", "external-123",
                null, null, null, AuditOutcome.Success, null, null, null, null),
            DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        var productRef = await dbContext.ProductRefs.AsNoTracking()
            .SingleOrDefaultAsync(p => p.ExternalProductId == "external-123");
        var items = await dbContext.AuditLogs.AsNoTracking()
            .Where(log => log.EntityType == "Product" && log.EntityId == "external-123")
            .ToListAsync();

        Assert.Null(productRef);
        Assert.Single(items);
    }
}
