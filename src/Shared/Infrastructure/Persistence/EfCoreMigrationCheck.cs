using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Shared.Kernel.Persistence;

namespace NafasLand.Admin.Shared.Infrastructure.Persistence;

/// <summary>
/// Generic implementation of <see cref="IMigrationCheck"/> for any DbContext.
/// Each module closes this over its own DbContext as <c>TContext</c> and
/// registers it in DI (ADR-041).
/// </summary>
public sealed class EfCoreMigrationCheck<TContext>(TContext dbContext) : IMigrationCheck
    where TContext : DbContext
{
    public async Task<MigrationCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        return new MigrationCheckResult(typeof(TContext).Name, pending);
    }
}
