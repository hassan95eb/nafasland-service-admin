using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.ToggleUserActive;

internal sealed class ToggleUserActiveCommandHandler(IdentityDbContext dbContext)
    : ICommandHandler<ToggleUserActiveCommand, ToggleUserActiveResult>
{
    public async Task<ToggleUserActiveResult> HandleAsync(ToggleUserActiveCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleAsync(u => u.Id == command.TargetUserId, cancellationToken);
        user.EnsureNotProtected("غیرفعال‌سازی");

        user.SetActive(!user.IsActive);
        return new ToggleUserActiveResult(user.IsActive);
    }
}
