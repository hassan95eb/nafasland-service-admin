using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Sample.Features.UnprotectedPing;

/// <summary>
/// این handler هرگز نباید اجرا شود؛ AuthorizationBehavior پیش از رسیدن به
/// اینجا رد می‌کند (نبود IRequiresPermission).
/// </summary>
internal sealed class UnprotectedPingCommandHandler : ICommandHandler<UnprotectedPingCommand, UnprotectedPingResult>
{
    public Task<UnprotectedPingResult> HandleAsync(UnprotectedPingCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(new UnprotectedPingResult("این پاسخ هرگز نباید دیده شود."));
    }
}
