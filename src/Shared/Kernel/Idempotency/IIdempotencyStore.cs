namespace NafasLand.Admin.Shared.Kernel.Idempotency;

public enum IdempotencyBeginOutcome
{
    Started,
    InProgress,
    Completed,
    KeyReused,
}

public sealed record IdempotencyBeginResult(
    IdempotencyBeginOutcome Outcome,
    string? ResponseJson = null);

/// <summary>
/// Persistence port used by the shared idempotency behavior. Catalog owns the
/// current implementation and table, while the pipeline stays module-agnostic.
/// </summary>
public interface IIdempotencyStore
{
    Task<IdempotencyBeginResult> TryBeginAsync(
        Guid key,
        Guid userId,
        string requestHash,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken);

    Task CompleteAsync(Guid key, string responseJson, CancellationToken cancellationToken);

    Task RemoveAsync(Guid key, CancellationToken cancellationToken);
}
