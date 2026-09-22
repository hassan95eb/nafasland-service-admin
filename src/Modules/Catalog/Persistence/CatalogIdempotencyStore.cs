using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Shared.Kernel.Idempotency;

namespace NafasLand.Admin.Modules.Catalog.Persistence;

internal sealed class CatalogIdempotencyStore(CatalogDbContext dbContext) : IIdempotencyStore
{
    public async Task<IdempotencyBeginResult> TryBeginAsync(
        Guid key,
        Guid userId,
        string requestHash,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        var candidate = IdempotencyRecord.Start(key, userId, requestHash, createdAt);
        dbContext.IdempotencyRecords.Add(candidate);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new IdempotencyBeginResult(IdempotencyBeginOutcome.Started);
        }
        catch (Exception exception) when (
            exception is DbUpdateException ||
            exception is ArgumentException &&
            string.Equals(
                dbContext.Database.ProviderName,
                "Microsoft.EntityFrameworkCore.InMemory",
                StringComparison.Ordinal))
        {
            dbContext.Entry(candidate).State = EntityState.Detached;
            var existing = await dbContext.IdempotencyRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(record => record.Key == key, cancellationToken);

            if (existing is null)
            {
                throw;
            }

            if (existing.UserId != userId || !string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                return new IdempotencyBeginResult(IdempotencyBeginOutcome.KeyReused);
            }

            return existing.Status == IdempotencyRecordStatus.Completed
                ? new IdempotencyBeginResult(IdempotencyBeginOutcome.Completed, existing.ResponseJson)
                : new IdempotencyBeginResult(IdempotencyBeginOutcome.InProgress);
        }
    }

    public async Task CompleteAsync(Guid key, string responseJson, CancellationToken cancellationToken)
    {
        var record = await dbContext.IdempotencyRecords.SingleAsync(item => item.Key == key, cancellationToken);
        record.Complete(responseJson);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid key, CancellationToken cancellationToken)
    {
        var record = await dbContext.IdempotencyRecords.SingleOrDefaultAsync(item => item.Key == key, cancellationToken);
        if (record is null)
        {
            return;
        }

        dbContext.IdempotencyRecords.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
