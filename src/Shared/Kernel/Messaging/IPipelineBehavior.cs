namespace NafasLand.Admin.Shared.Kernel.Messaging;

/// <summary>
/// ادامهٔ pipeline پس از behavior فعلی؛ آخرین حلقه، فراخوانی handler است.
/// </summary>
public delegate Task<TResponse> CommandHandlerDelegate<TResponse>();

/// <summary>
/// یک حلقه از pipeline اجباری (ADR-006). ترتیب اجرا با ترتیب ثبت در DI تعیین
/// می‌شود: Logging → Validation → Authorization → Transaction → Audit → Handler.
/// </summary>
public interface IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CommandHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
