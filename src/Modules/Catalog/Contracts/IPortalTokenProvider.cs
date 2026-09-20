namespace NafasLand.Admin.Modules.Catalog.Contracts;

public interface IPortalTokenProvider
{
    public Task<string> GetTokenAsync(CancellationToken cancellationToken);
}
