using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Kernel.Tests.Messaging;

public sealed class AuditBehaviorTests
{
    private sealed record FakeCommand : ICommand<string>;

    [Fact]
    public async Task این_گام_فقط_عبور_می‌دهد_و_نتیجهٔ_next_را_برمی‌گرداند()
    {
        var behavior = new AuditBehavior<FakeCommand, string>();

        var result = await behavior.HandleAsync(new FakeCommand(), () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }
}
