using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NafasLand.Admin.Shared.Kernel.Persistence;

namespace NafasLand.Admin.Modules.Catalog.Persistence;

internal sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options), IUnitOfWork
{
    public const string SchemaName = "catalog";

    private IDbContextTransaction? _currentTransaction;

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        await SaveChangesAsync(cancellationToken);
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.CommitAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.RollbackAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }
}
