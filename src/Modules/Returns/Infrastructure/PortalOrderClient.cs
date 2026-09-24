using System.Globalization;
using System.Net;
using System.Text.Json;
using NafasLand.Admin.Shared.Infrastructure.Portal;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Returns.Infrastructure;

/// <summary>Read-only: the one order endpoint ADR-054 brings into scope. Nothing here writes to the portal.</summary>
internal interface IPortalOrderClient
{
    /// <returns>null when the order does not exist (404 or <c>success: false</c>).</returns>
    Task<PortalOrder?> GetOrderAsync(long orderId, CancellationToken cancellationToken);
}

internal sealed class PortalOrderClient(HttpClient httpClient) : IPortalOrderClient
{
    public async Task<PortalOrder?> GetOrderAsync(long orderId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"store/orders/{orderId.ToString(CultureInfo.InvariantCulture)}");
        using var response = await PortalHttp.SendAsync(httpClient, request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode && PortalHttp.IsExplicitFailure(json))
        {
            return null;
        }

        PortalHttp.EnsureSuccess(response, json);

        try
        {
            return PortalOrderMapper.Map(json);
        }
        catch (JsonException exception)
        {
            throw new PortalUnavailableException(exception);
        }
    }
}
