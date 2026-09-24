namespace NafasLand.Admin.Shared.Kernel.Users;

/// <summary>
/// Read-only id → username lookup, implemented by Identity and consumed by
/// report views (ADR-014) that only hold a user id. A missing id (unknown or
/// removed user) is simply absent from the result, never an error.
/// </summary>
public interface IUserDirectory
{
    Task<IReadOnlyDictionary<Guid, string>> GetUsernamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
