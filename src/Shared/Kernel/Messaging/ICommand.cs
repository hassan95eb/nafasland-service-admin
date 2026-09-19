namespace NafasLand.Admin.Shared.Kernel.Messaging;

/// <summary>
/// A mutating operation that flows through the mandatory pipeline (ADR-006).
/// </summary>
/// <typeparam name="TResponse">The handler's response type.</typeparam>
public interface ICommand<TResponse>
{
}
