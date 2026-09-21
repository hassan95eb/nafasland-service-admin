using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Catalog.Persistence;

namespace NafasLand.Admin.Modules.Catalog.Jobs;

internal sealed class IdempotencyPurgeJob(CatalogDbContext dbContext, TimeProvider timeProvider)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow() - IdempotencyRetentionPolicy.RetentionPeriod;
        var expired = await dbContext.IdempotencyRecords
            .Where(record => record.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            return;
        }

        dbContext.IdempotencyRecords.RemoveRange(expired);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
