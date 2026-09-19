using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// The first link in the pipeline (ADR-006). Logs the start and end of a
/// command's execution along with its duration; CorrelationId is added to these
/// log entries automatically via LogContext (the middleware).
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
