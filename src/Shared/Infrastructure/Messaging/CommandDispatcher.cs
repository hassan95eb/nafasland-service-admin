using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// ثبت به‌صورت Scoped می‌شود؛ <paramref name="serviceProvider"/> همان scope
/// درخواست جاری است، پس همهٔ سرویس‌های scoped (مثل DbContext) که در طول این
/// pipeline resolve می‌شوند، با بقیهٔ درخواست یکی‌اند.
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
