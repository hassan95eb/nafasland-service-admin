namespace NafasLand.Admin.Shared.Kernel.Persistence;

/// <summary>
/// تراکنش دیتابیس یک ماژول، مستقل از EF Core، برای استفادهٔ TransactionBehavior
/// (ADR-006). هر ماژول DbContext خودش را به این رابط map می‌کند و آن را با
/// کلید نام ماژول در DI ثبت می‌کند تا در حضور چند ماژول دیگر مبهم نباشد.
/// </summary>
public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken cancellationToken);

    Task CommitTransactionAsync(CancellationToken cancellationToken);

    Task RollbackTransactionAsync(CancellationToken cancellationToken);
}
