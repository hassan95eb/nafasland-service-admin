using FluentValidation;

namespace NafasLand.Admin.Modules.Identity.Features.Login;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Username).NotEmpty().WithMessage("نام کاربری نمی‌تواند خالی باشد.");
        RuleFor(command => command.Password).NotEmpty().WithMessage("رمز عبور نمی‌تواند خالی باشد.");
    }
}
