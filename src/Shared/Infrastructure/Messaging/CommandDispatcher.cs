using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// Registered as Scoped; <paramref name="serviceProvider"/> is the current
/// request's own scope, so every scoped service (like a DbContext) resolved
/// during this pipeline is the same instance as the rest of the request.
/// </summary>
internal sealed class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    public Task<TResponse> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>
    {
        var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        CommandHandlerDelegate<TResponse> pipeline = () => handler.HandleAsync(command, cancellationToken);

        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TCommand, TResponse>>().Reverse();
        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(command, next, cancellationToken);
        }

        return pipeline();
    }
}
