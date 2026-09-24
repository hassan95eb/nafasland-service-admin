using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Sample.Features.Ping;
using NafasLand.Admin.Modules.Sample.Persistence;
using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Sample.Tests.Features.Ping;

public sealed class PingCommandHandlerTests
{
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeAuditContext : IAuditContext
    {
        public void SetEntityId(string entityId)
        {
        }

        public void SetParentEntity(string entityType, string entityId)
        {
        }

        public void SetBefore(object? snapshot)
        {
        }

        public void SetAfter(object? snapshot)
        {
        }
    }

    // Never opens a real connection: Add only puts the record in the
    // ChangeTracker, which is the only thing the handler does (SaveChanges is
    // TransactionBehavior/commit's job, not the handler's).
    private static SampleDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseSqlServer("Server=fake;Database=fake;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new SampleDbContext(options);
    }

    [Fact]
    public async Task رکورد_جدید_را_به_ChangeTracker_اضافه_می‌کند()
    {
        using var dbContext = CreateDbContext();
        var fixedNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var handler = new PingCommandHandler(dbContext, new FixedTimeProvider(fixedNow), new FakeAuditContext());

        var result = await handler.HandleAsync(new PingCommand("سلام"), CancellationToken.None);

        var tracked = Assert.Single(dbContext.ChangeTracker.Entries<PingRecord>());
        Assert.Equal("سلام", tracked.Entity.Message);
        Assert.Equal(fixedNow.UtcDateTime, tracked.Entity.CreatedAtUtc);
        Assert.Equal(tracked.Entity.Id, result.Id);
        Assert.Equal("سلام", result.Message);
    }
}
