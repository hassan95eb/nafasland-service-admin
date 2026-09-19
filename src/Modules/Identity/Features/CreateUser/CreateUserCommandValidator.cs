using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Modules.Identity.Security;

namespace NafasLand.Admin.Modules.Identity.Features.CreateUser;

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IdentityDbContext dbContext)
    {
        RuleFor(command => command.Username)
            .NotEmpty().WithMessage("نام کاربری نمی‌تواند خالی باشد.")
            .MaximumLength(100).WithMessage("نام کاربری نمی‌تواند بیش از ۱۰۰ نویسه باشد.")
            .MustAsync((username, cancellationToken) =>
                dbContext.Users.AllAsync(user => user.Username != username, cancellationToken))
            .WithMessage("این نام کاربری قبلاً استفاده شده است.");

        RuleFor(command => command.InitialPassword)
            .NotEmpty().WithMessage("رمز عبور اولیه نمی‌تواند خالی باشد.")
            .MinimumLength(PasswordPolicy.MinLength).WithMessage($"رمز عبور اولیه باید حداقل {PasswordPolicy.MinLength} نویسه باشد.");
    }
}
