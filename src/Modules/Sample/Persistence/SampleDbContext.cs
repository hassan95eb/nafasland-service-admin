using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NafasLand.Admin.Shared.Kernel.Persistence;

namespace NafasLand.Admin.Modules.Sample.Persistence;

/// <summary>
/// Each module has its own database schema (ADR-004, ADR-019).
/// </summary>
internal sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options), IUnitOfWork
{
    public const string SchemaName = "sample";

    private IDbContextTransaction? _currentTransaction;

    public DbSet<PingRecord> PingRecords => Set<PingRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SampleDbContext).Assembly);
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
