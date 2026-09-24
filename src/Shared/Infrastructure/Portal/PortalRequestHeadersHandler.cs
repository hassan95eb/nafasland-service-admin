using System.Net.Http.Headers;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;

namespace NafasLand.Admin.Shared.Infrastructure.Portal;

/// <summary>
/// Attaches the bearer token and the correlation id to every portal attempt.
/// The token only ever lives on the outgoing request; the "Authorization"
/// header is redacted from HttpClient logging (ADR-007, ADR-039).
/// </summary>
internal sealed class PortalRequestHeadersHandler(
    IPortalTokenProvider tokenProvider,
    ICorrelationIdAccessor correlationIdAccessor) : DelegatingHandler
{
    public const string CorrelationIdHeader = "X-Correlation-Id";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Remove(CorrelationIdHeader);
        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, correlationIdAccessor.CorrelationId);
        return await base.SendAsync(request, cancellationToken);
    }
}
