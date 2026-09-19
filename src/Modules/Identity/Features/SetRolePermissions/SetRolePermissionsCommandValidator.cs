using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;

namespace NafasLand.Admin.Modules.Identity.Features.SetRolePermissions;

internal sealed class SetRolePermissionsCommandValidator : AbstractValidator<SetRolePermissionsCommand>
{
    public SetRolePermissionsCommandValidator(IdentityDbContext dbContext)
    {
        RuleFor(command => command.RoleId)
            .MustAsync((roleId, cancellationToken) => dbContext.Roles.AnyAsync(r => r.Id == roleId, cancellationToken))
            .WithMessage("نقش یافت نشد.");

        RuleFor(command => command.PermissionKeys)
            .MustAsync(async (keys, cancellationToken) =>
            {
                var distinctKeys = keys.Distinct().ToList();
                var existingCount = await dbContext.Permissions.CountAsync(p => distinctKeys.Contains(p.Key), cancellationToken);
                return existingCount == distinctKeys.Count;
            })
            .WithMessage("یک یا چند permission انتخاب‌شده یافت نشد.");
    }
}
