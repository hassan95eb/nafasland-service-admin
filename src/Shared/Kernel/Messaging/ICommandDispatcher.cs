namespace NafasLand.Admin.Shared.Kernel.Messaging;

/// <summary>
/// Entry point for endpoints into the mandatory pipeline. Both generic parameters
/// must be given explicitly, since TResponse cannot be inferred from the
/// interface constraint.
/// </summary>
public interface ICommandDispatcher
{
    Task<TResponse> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>;
}
