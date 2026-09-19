using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// دومین حلقهٔ pipeline (ADR-006). اگر برای command ای validator ثبت نشده
/// باشد، بدون خطا رد می‌شود؛ نه هر commandی الزاماً قاعدهٔ اعتبارسنجی دارد.
/// </summary>
internal sealed class ValidationBehavior<TCommand, TResponse>(IServiceProvider serviceProvider)
    : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validator = serviceProvider.GetService<IValidator<TCommand>>();
        if (validator is null)
        {
            return await next();
        }

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).ToArray());

            throw new CommandValidationException(errors);
        }

        return await next();
    }
}
