using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// The fifth link in the pipeline (ADR-006) — deliberately just a skeleton for
/// now. Its place in the pipeline is fixed from this step so the Auditing module
/// can be added in step 2 (ADR-009) without reordering the other behaviors. For
/// now it simply passes through.
/// </summary>
internal sealed class AuditBehavior<TCommand, TResponse> : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public Task<TResponse> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        return next();
    }
}
