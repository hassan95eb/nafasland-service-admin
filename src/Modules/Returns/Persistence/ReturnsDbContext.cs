using Microsoft.EntityFrameworkCore;

namespace NafasLand.Admin.Modules.Returns.Persistence;

/// <summary>
/// The module's own schema (ADR-054). It has no command of its own — filing
/// goes through Approvals — so it is not an IUnitOfWork; the only writer is
/// the approval executor.
/// </summary>
internal sealed class ReturnsDbContext(DbContextOptions<ReturnsDbContext> options) : DbContext(options)
{
    public const string SchemaName = "returns";

    public DbSet<ReturnRecord> ReturnRecords => Set<ReturnRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReturnsDbContext).Assembly);
    }
}
