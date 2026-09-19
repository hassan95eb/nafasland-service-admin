using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Sample.Features.UnprotectedPing;

/// <summary>
/// This handler should never run; AuthorizationBehavior rejects the command
/// before it gets here (no IRequiresPermission).
/// </summary>
internal sealed class UnprotectedPingCommandHandler : ICommandHandler<UnprotectedPingCommand, UnprotectedPingResult>
{
    public Task<UnprotectedPingResult> HandleAsync(UnprotectedPingCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(new UnprotectedPingResult("این پاسخ هرگز نباید دیده شود."));
    }
}
