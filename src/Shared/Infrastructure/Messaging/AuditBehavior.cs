using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// پنجمین حلقهٔ pipeline (ADR-006) — عمداً فقط یک اسکلت است. جایگاهش در
/// pipeline از همین گام تثبیت می‌شود تا ماژول Auditing در گام ۲ (ADR-009)
/// بدون جابه‌جایی ترتیب سایر behaviorها اضافه شود. فعلاً فقط عبور می‌دهد.
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
