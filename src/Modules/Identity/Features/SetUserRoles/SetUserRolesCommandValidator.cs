using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;

namespace NafasLand.Admin.Modules.Identity.Features.SetUserRoles;

internal sealed class SetUserRolesCommandValidator : AbstractValidator<SetUserRolesCommand>
{
    public SetUserRolesCommandValidator(IdentityDbContext dbContext)
    {
        RuleFor(command => command.TargetUserId)
            .MustAsync((userId, cancellationToken) => dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken))
            .WithMessage("کاربر یافت نشد.");

        RuleFor(command => command.RoleIds)
            .MustAsync(async (roleIds, cancellationToken) =>
            {
                var distinctIds = roleIds.Distinct().ToList();
                var existingCount = await dbContext.Roles.CountAsync(r => distinctIds.Contains(r.Id), cancellationToken);
                return existingCount == distinctIds.Count;
            })
            .WithMessage("یک یا چند نقش انتخاب‌شده یافت نشد.");
    }
}
