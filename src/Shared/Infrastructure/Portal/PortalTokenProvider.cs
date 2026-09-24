using Microsoft.Extensions.Options;

namespace NafasLand.Admin.Shared.Infrastructure.Portal;

internal sealed class PortalTokenProvider(IOptions<PortalConnectionOptions> options) : IPortalTokenProvider
{
    private readonly string _token = options.Value.BearerToken;

    public Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_token);
    }
}
