using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Sample.Features.Ping;
using NafasLand.Admin.Modules.Sample.Persistence;

namespace NafasLand.Admin.Modules.Sample.Tests.Features.Ping;

public sealed class PingCommandHandlerTests
{
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    // بدون باز شدن اتصال واقعی: Add فقط رکورد را در ChangeTracker می‌گذارد؛
    // این تنها چیزی است که handler انجام می‌دهد (SaveChanges کار
    // TransactionBehavior/commit تراکنش است، نه handler).
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
        var handler = new PingCommandHandler(dbContext, new FixedTimeProvider(fixedNow));

        var result = await handler.HandleAsync(new PingCommand("سلام"), CancellationToken.None);

        var tracked = Assert.Single(dbContext.ChangeTracker.Entries<PingRecord>());
        Assert.Equal("سلام", tracked.Entity.Message);
        Assert.Equal(fixedNow.UtcDateTime, tracked.Entity.CreatedAtUtc);
        Assert.Equal(tracked.Entity.Id, result.Id);
        Assert.Equal("سلام", result.Message);
    }
}
