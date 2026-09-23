using NafasLand.Admin.Shared.Infrastructure.CorrelationId;

namespace NafasLand.Admin.Modules.Approvals.Tests.Fakes;

internal sealed class FakeCorrelationIdAccessor(string correlationId = "test-correlation-id") : ICorrelationIdAccessor
{
    public string CorrelationId { get; } = correlationId;
}
