using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Shared.Kernel.Users;

namespace NafasLand.Admin.Modules.Identity.Persistence;

/// <summary>Only ever exposes id → username; nothing else about the account leaves this module through here.</summary>
internal sealed class UserDirectory(IdentityDbContext dbContext) : IUserDirectory
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetUsernamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var ids = userIds.Distinct().ToList();
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.Username, cancellationToken);
    }
}
