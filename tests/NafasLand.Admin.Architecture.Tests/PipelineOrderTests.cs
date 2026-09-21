using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Infrastructure.Extensions;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Architecture.Tests;

/// <summary>
/// Guarantees the mandatory pipeline's exact order (ADR-006) against the real
/// AddSharedInfrastructure registration: Logging → Validation → Authorization →
/// Idempotency → Transaction → Audit.
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
                "IdempotencyBehavior`2",
                "TransactionBehavior`2",
                "AuditBehavior`2",
            ],
            behaviorTypeNames);
    }
}
