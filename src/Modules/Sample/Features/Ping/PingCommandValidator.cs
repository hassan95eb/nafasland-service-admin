using FluentValidation;

namespace NafasLand.Admin.Modules.Sample.Features.Ping;

internal sealed class PingCommandValidator : AbstractValidator<PingCommand>
{
    public PingCommandValidator()
    {
        RuleFor(command => command.Message)
            .NotEmpty().WithMessage("پیام نمی‌تواند خالی باشد.")
            .MaximumLength(200).WithMessage("پیام نمی‌تواند بیش از ۲۰۰ نویسه باشد.");
    }
}
