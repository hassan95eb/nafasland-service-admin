using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// اولین حلقهٔ pipeline (ADR-006). شروع و پایان اجرای command را با مدت زمان
/// لاگ می‌کند؛ CorrelationId از طریق LogContext (میان‌افزار) به‌طور خودکار
/// به این لاگ‌ها اضافه می‌شود.
/// </summary>
internal sealed class LoggingBehavior<TCommand, TResponse>(ILogger<LoggingBehavior<TCommand, TResponse>> logger)
    : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        var moduleName = ModuleNameResolver.ResolveFromNamespace(typeof(TCommand).Namespace) ?? "نامشخص";
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("شروع اجرای {ModuleName}/{CommandName}", moduleName, commandName);

        try
        {
            var response = await next();
            logger.LogInformation(
                "پایان موفق {ModuleName}/{CommandName} در {ElapsedMilliseconds} میلی‌ثانیه",
                moduleName,
                commandName,
                stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "خطا در اجرای {ModuleName}/{CommandName} پس از {ElapsedMilliseconds} میلی‌ثانیه",
                moduleName,
                commandName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
