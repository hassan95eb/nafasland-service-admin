using FluentValidation;
using NafasLand.Admin.Modules.Identity.Security;

namespace NafasLand.Admin.Modules.Identity.Features.ChangePassword;

internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword).NotEmpty().WithMessage("رمز عبور فعلی نمی‌تواند خالی باشد.");

        RuleFor(command => command.NewPassword)
            .NotEmpty().WithMessage("رمز عبور جدید نمی‌تواند خالی باشد.")
            .MinimumLength(PasswordPolicy.MinLength).WithMessage($"رمز عبور جدید باید حداقل {PasswordPolicy.MinLength} نویسه باشد.")
            .NotEqual(command => command.CurrentPassword).WithMessage("رمز عبور جدید باید با رمز فعلی متفاوت باشد.");
    }
}
