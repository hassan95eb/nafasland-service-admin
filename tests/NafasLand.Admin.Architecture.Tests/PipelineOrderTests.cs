using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Infrastructure.Extensions;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Architecture.Tests;

/// <summary>
/// ترتیب دقیق pipeline اجباری (ADR-006) را روی رجیستریشن واقعی
/// AddSharedInfrastructure تضمین می‌کند: Logging → Validation →
/// Authorization → Transaction → Audit.
/// </summary>
public sealed class PipelineOrderTests
{
    private sealed record FakeCommand : ICommand<string>;

    [Fact]
    public void ترتیب_ثبت_behaviorها_دقیقاً_طبق_ADR_۰۰۶_است()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSharedInfrastructure();
        var provider = services.BuildServiceProvider();

        var behaviorTypeNames = provider
            .GetServices<IPipelineBehavior<FakeCommand, string>>()
            .Select(behavior => behavior.GetType().Name)
            .ToList();

        Assert.Equal(
            [
                "LoggingBehavior`2",
                "ValidationBehavior`2",
                "AuthorizationBehavior`2",
                "TransactionBehavior`2",
                "AuditBehavior`2",
            ],
            behaviorTypeNames);
    }
}
