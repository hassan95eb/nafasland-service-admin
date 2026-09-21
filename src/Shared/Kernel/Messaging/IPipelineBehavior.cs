namespace NafasLand.Admin.Shared.Kernel.Messaging;

/// <summary>
/// Continuation of the pipeline after the current behavior; the final link calls
/// the handler.
/// </summary>
public delegate Task<TResponse> CommandHandlerDelegate<TResponse>();

/// <summary>
/// One link in the mandatory pipeline (ADR-006). Execution order is determined by
/// DI registration order: Logging → Validation → Authorization → Idempotency →
/// Transaction → Audit → Handler.
/// </summary>
public interface IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CommandHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
