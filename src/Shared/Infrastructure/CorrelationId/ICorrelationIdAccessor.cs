namespace NafasLand.Admin.Shared.Infrastructure.CorrelationId;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; }
}
