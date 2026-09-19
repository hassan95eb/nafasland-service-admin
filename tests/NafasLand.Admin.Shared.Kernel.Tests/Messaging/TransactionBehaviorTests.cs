using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Modules.FakeModuleForTests;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Persistence;

namespace NafasLand.Admin.Shared.Kernel.Tests.Messaging;

public sealed class TransactionBehaviorTests
{
    private sealed record NonModuleCommand : ICommand<string>;

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public List<string> Calls { get; } = [];

        public Task BeginTransactionAsync(CancellationToken cancellationToken)
        {
            Calls.Add("begin");
            return Task.CompletedTask;
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken)
        {
            Calls.Add("commit");
            return Task.CompletedTask;
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken)
        {
            Calls.Add("rollback");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task بدون_ماژول_متناظر_با_namespace_command_بدون_تراکنش_رد_می‌شود()
    {
        var services = new ServiceCollection();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        var provider = services.BuildServiceProvider();
        var behavior = provider.GetRequiredService<IPipelineBehavior<NonModuleCommand, string>>();

        var result = await behavior.HandleAsync(new NonModuleCommand(), () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task با_IUnitOfWork_ثبت‌شده_اجرای_موفق_commit_می‌کند()
    {
        var fakeUnitOfWork = new FakeUnitOfWork();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IUnitOfWork>("FakeModuleForTests", fakeUnitOfWork);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        var provider = services.BuildServiceProvider();

        var behavior = provider
            .GetRequiredService<IPipelineBehavior<FakeModuleCommand, string>>();

        var result = await behavior.HandleAsync(
            new FakeModuleCommand(),
            () => Task.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(["begin", "commit"], fakeUnitOfWork.Calls);
    }

    [Fact]
    public async Task با_IUnitOfWork_ثبت‌شده_شکست_handler_rollback_می‌کند()
    {
        var fakeUnitOfWork = new FakeUnitOfWork();
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IUnitOfWork>("FakeModuleForTests", fakeUnitOfWork);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        var provider = services.BuildServiceProvider();

        var behavior = provider
            .GetRequiredService<IPipelineBehavior<FakeModuleCommand, string>>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.HandleAsync(
            new FakeModuleCommand(),
            () => throw new InvalidOperationException("خطای دلخواه handler"),
            CancellationToken.None));

        Assert.Equal(["begin", "rollback"], fakeUnitOfWork.Calls);
    }
}
