namespace NafasLand.Admin.Shared.Infrastructure.Portal;

public interface IPortalTokenProvider
{
    public Task<string> GetTokenAsync(CancellationToken cancellationToken);
}
