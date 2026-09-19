using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NafasLand.Admin.Shared.Kernel.Persistence;

namespace NafasLand.Admin.Modules.Auditing.Persistence;

/// <summary>Own schema per module (ADR-004, ADR-019, ADR-045).</summary>
internal sealed class AuditingDbContext(DbContextOptions<AuditingDbContext> options) : DbContext(options), IUnitOfWork
{
    public const string SchemaName = "audit";

    private IDbContextTransaction? _currentTransaction;

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ProductRef> ProductRefs => Set<ProductRef>();

    public DbSet<AuditExportJobRecord> AuditExportJobs => Set<AuditExportJobRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditingDbContext).Assembly);
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
