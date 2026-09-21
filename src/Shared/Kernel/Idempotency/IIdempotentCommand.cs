namespace NafasLand.Admin.Shared.Kernel.Idempotency;

/// <summary>
/// Marks a command whose result must be replayed for the same Idempotency-Key.
/// </summary>
public interface IIdempotentCommand
{
}
