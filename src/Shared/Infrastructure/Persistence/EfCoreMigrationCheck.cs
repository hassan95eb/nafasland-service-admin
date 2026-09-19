using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Shared.Kernel.Persistence;

namespace NafasLand.Admin.Shared.Infrastructure.Persistence;

/// <summary>
/// پیاده‌سازی عمومی <see cref="IMigrationCheck"/> برای هر DbContext. هر ماژول
/// این را برای DbContext خودش با <c>TContext</c> بسته می‌کند و در DI ثبت
/// می‌کند (ADR-041).
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
