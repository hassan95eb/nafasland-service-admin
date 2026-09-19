namespace NafasLand.Admin.Shared.Kernel.Persistence;

/// <summary>
/// A module's database transaction, independent of EF Core, for use by
/// TransactionBehavior (ADR-006). Each module maps its own DbContext to this
/// interface and registers it in DI keyed by the module name, so resolution
/// stays unambiguous once other modules exist.
/// </summary>
public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken cancellationToken);

    Task CommitTransactionAsync(CancellationToken cancellationToken);

    Task RollbackTransactionAsync(CancellationToken cancellationToken);
}
