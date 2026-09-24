using System.Text.Json;
using NafasLand.Admin.Shared.Kernel.Errors;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace NafasLand.Admin.Shared.Infrastructure.Portal;

/// <summary>
/// Turns transport failures and portal-side errors into the panel's own safe
/// exceptions, so no typed portal client ever lets a raw portal message or a
/// Polly exception reach the user (ADR-036).
/// </summary>
public static class PortalHttp
{
    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient httpClient,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (PortalBusyException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or TimeoutRejectedException
            or BrokenCircuitException
            or ExecutionRejectedException)
        {
            throw new PortalUnavailableException(exception);
        }
    }

    public static void EnsureSuccess(HttpResponseMessage response, string json)
    {
        if (!response.IsSuccessStatusCode || IsExplicitFailure(json))
        {
            throw new PortalUnavailableException();
        }
    }

    /// <summary>The portal sometimes answers 200 with <c>{"success": false}</c>.</summary>
    public static bool IsExplicitFailure(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("success", out var success)
                && success.ValueKind == JsonValueKind.False;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
