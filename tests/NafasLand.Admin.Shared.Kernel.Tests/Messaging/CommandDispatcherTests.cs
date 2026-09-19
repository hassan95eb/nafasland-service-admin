using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Kernel.Tests.Messaging;

public sealed class CommandDispatcherTests
{
    private sealed record FakeCommand(string Input) : ICommand<string>;

    private sealed class FakeHandler : ICommandHandler<FakeCommand, string>
    {
        public Task<string> HandleAsync(FakeCommand command, CancellationToken cancellationToken)
            => Task.FromResult(command.Input + "-handled");
    }

    private sealed class RecordingBehavior(string name, List<string> callOrder) : IPipelineBehavior<FakeCommand, string>
    {
        public async Task<string> HandleAsync(
            FakeCommand command,
            CommandHandlerDelegate<string> next,
            CancellationToken cancellationToken)
        {
            callOrder.Add($"{name}-before");
            var result = await next();
            callOrder.Add($"{name}-after");
            return result;
        }
    }

    [Fact]
    public async Task SendAsync_بدون_behavior_مستقیم_handler_را_صدا_می‌زند()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICommandHandler<FakeCommand, string>, FakeHandler>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        var provider = services.BuildServiceProvider();

        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();
        var result = await dispatcher.SendAsync<FakeCommand, string>(new FakeCommand("x"), CancellationToken.None);

        Assert.Equal("x-handled", result);
    }

    [Fact]
    public async Task SendAsync_behaviorها_را_دقیقاً_به_ترتیب_ثبت_اجرا_می‌کند()
    {
        var callOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddScoped<ICommandHandler<FakeCommand, string>, FakeHandler>();
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<IPipelineBehavior<FakeCommand, string>>(new RecordingBehavior("first", callOrder));
        services.AddSingleton<IPipelineBehavior<FakeCommand, string>>(new RecordingBehavior("second", callOrder));
        var provider = services.BuildServiceProvider();

        var dispatcher = provider.GetRequiredService<ICommandDispatcher>();
        await dispatcher.SendAsync<FakeCommand, string>(new FakeCommand("x"), CancellationToken.None);

        Assert.Equal(["first-before", "second-before", "second-after", "first-after"], callOrder);
    }
}
