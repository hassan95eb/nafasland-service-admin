using NafasLand.Admin.Shared.Kernel.Users;

namespace NafasLand.Admin.Shared.Infrastructure.Users;

/// <summary>Knows no one — with Identity disabled every actor simply shows as unknown, never an error.</summary>
internal sealed class NullUserDirectory : IUserDirectory
{
    public Task<IReadOnlyDictionary<Guid, string>> GetUsernamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
}
