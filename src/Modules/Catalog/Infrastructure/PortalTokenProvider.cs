using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;

namespace NafasLand.Admin.Modules.Catalog.Infrastructure;

internal sealed class PortalTokenProvider(IOptions<PortalOptions> options) : IPortalTokenProvider
{
    private readonly string _token = options.Value.BearerToken;

    public Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_token);
    }
}
