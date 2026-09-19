namespace NafasLand.Admin.Shared.Kernel.Messaging;

/// <summary>
/// نقطهٔ ورود endpoint ها به pipeline اجباری. هر دو پارامتر جنریک صریح داده
/// می‌شوند چون TResponse از روی محدودیت interface قابل استنتاج نیست.
/// </summary>
public interface ICommandDispatcher
{
    Task<TResponse> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>;
}
