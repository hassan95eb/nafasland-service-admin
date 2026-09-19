using FluentValidation;
using NafasLand.Admin.Modules.Identity.Security;

namespace NafasLand.Admin.Modules.Identity.Features.ResetPassword;

internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.TargetUserId).NotEmpty();

        RuleFor(command => command.NewPassword)
            .NotEmpty().WithMessage("رمز عبور جدید نمی‌تواند خالی باشد.")
            .MinimumLength(PasswordPolicy.MinLength).WithMessage($"رمز عبور جدید باید حداقل {PasswordPolicy.MinLength} نویسه باشد.");
    }
}
