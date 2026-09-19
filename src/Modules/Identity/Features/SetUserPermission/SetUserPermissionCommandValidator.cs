using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;

namespace NafasLand.Admin.Modules.Identity.Features.SetUserPermission;

internal sealed class SetUserPermissionCommandValidator : AbstractValidator<SetUserPermissionCommand>
{
    public SetUserPermissionCommandValidator(IdentityDbContext dbContext)
    {
        RuleFor(command => command.TargetUserId)
            .MustAsync((userId, cancellationToken) => dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken))
            .WithMessage("کاربر یافت نشد.");

        RuleFor(command => command.PermissionKey)
            .MustAsync((key, cancellationToken) => dbContext.Permissions.AnyAsync(p => p.Key == key, cancellationToken))
            .WithMessage("permission یافت نشد.");
    }
}
