using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NafasLand.Admin.Modules.Catalog.Persistence;
using NafasLand.Admin.Shared.Kernel.Idempotency;

namespace NafasLand.Admin.Modules.Catalog.Tests.Persistence;

public sealed class CatalogIdempotencyStoreTests
{
    [Fact]
    public void کلید_idempotency_کلید_اصلی_دیتابیس_است()
    {
        using var context = CreateContext(Guid.NewGuid().ToString(), new InMemoryDatabaseRoot());
        var entity = context.Model.FindEntityType(typeof(IdempotencyRecord));

        var primaryKey = Assert.Single(entity!.GetKeys(), key => key.IsPrimaryKey());
        Assert.Equal(nameof(IdempotencyRecord.Key), Assert.Single(primaryKey.Properties).Name);
    }

    [Fact]
    public async Task درج_همزمان_یک_کلید_از_یکتایی_دیتابیس_به_InProgress_می‌رسد()
    {
        var databaseName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        await using var firstContext = CreateContext(databaseName, root);
        await using var secondContext = CreateContext(databaseName, root);
        var key = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var first = await new CatalogIdempotencyStore(firstContext).TryBeginAsync(
            key, userId, "HASH", DateTimeOffset.UtcNow, CancellationToken.None);
        var second = await new CatalogIdempotencyStore(secondContext).TryBeginAsync(
            key, userId, "HASH", DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(IdempotencyBeginOutcome.Started, first.Outcome);
        Assert.Equal(IdempotencyBeginOutcome.InProgress, second.Outcome);
        Assert.Single(await firstContext.IdempotencyRecords.ToListAsync());
    }

    [Fact]
    public async Task رکورد_تکمیل_شده_پاسخ_قبلی_را_برمی‌گرداند()
    {
        var databaseName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        await using var firstContext = CreateContext(databaseName, root);
        var key = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var firstStore = new CatalogIdempotencyStore(firstContext);

        await firstStore.TryBeginAsync(key, userId, "HASH", DateTimeOffset.UtcNow, CancellationToken.None);
        await firstStore.CompleteAsync(key, "{\"value\":1}", CancellationToken.None);

        await using var secondContext = CreateContext(databaseName, root);
        var replay = await new CatalogIdempotencyStore(secondContext).TryBeginAsync(
            key, userId, "HASH", DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(IdempotencyBeginOutcome.Completed, replay.Outcome);
        Assert.Equal("{\"value\":1}", replay.ResponseJson);
    }

    private static CatalogDbContext CreateContext(string databaseName, InMemoryDatabaseRoot root)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName, root)
            .Options;
        return new CatalogDbContext(options);
    }
}
