using NafasLand.Admin.Shared.Kernel.Users;

namespace NafasLand.Admin.Modules.Auditing.Tests;

internal sealed class FakeUserDirectory(IReadOnlyDictionary<Guid, string>? usernames = null) : IUserDirectory
{
    private readonly IReadOnlyDictionary<Guid, string> _usernames = usernames ?? new Dictionary<Guid, string>();

    public Task<IReadOnlyDictionary<Guid, string>> GetUsernamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(
            _usernames.Where(pair => userIds.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value));
}
