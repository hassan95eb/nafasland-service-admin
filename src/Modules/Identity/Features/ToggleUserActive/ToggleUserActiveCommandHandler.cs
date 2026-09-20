using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.ToggleUserActive;

internal sealed class ToggleUserActiveCommandHandler(IdentityDbContext dbContext, IAuditContext auditContext)
    : ICommandHandler<ToggleUserActiveCommand, ToggleUserActiveResult>
{
    public async Task<ToggleUserActiveResult> HandleAsync(ToggleUserActiveCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleAsync(u => u.Id == command.TargetUserId, cancellationToken);
        auditContext.SetEntityId(user.Id.ToString());
        auditContext.SetBefore(new { user.IsActive });
        user.EnsureNotProtected("غیرفعال‌سازی");

        user.SetActive(!user.IsActive);
        auditContext.SetAfter(new { user.IsActive });
        return new ToggleUserActiveResult(user.IsActive);
    }
}
