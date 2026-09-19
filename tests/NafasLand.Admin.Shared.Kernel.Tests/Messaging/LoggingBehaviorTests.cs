using Microsoft.Extensions.Logging.Abstractions;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Kernel.Tests.Messaging;

public sealed class LoggingBehaviorTests
{
    private sealed record FakeCommand : ICommand<string>;

    [Fact]
    public async Task نتیجهٔ_موفق_next_را_بدون_تغییر_برمی‌گرداند()
    {
        var behavior = new LoggingBehavior<FakeCommand, string>(NullLogger<LoggingBehavior<FakeCommand, string>>.Instance);

        var result = await behavior.HandleAsync(new FakeCommand(), () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task استثنای_next_را_دوباره_پرتاب_می‌کند()
    {
        var behavior = new LoggingBehavior<FakeCommand, string>(NullLogger<LoggingBehavior<FakeCommand, string>>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.HandleAsync(new FakeCommand(), () => throw new InvalidOperationException(), CancellationToken.None));
    }
}
