using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Kernel.Errors;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace NafasLand.Admin.Modules.Catalog.Infrastructure;

internal sealed class PortalProductClient(
    HttpClient httpClient,
    IPortalTokenProvider tokenProvider,
    ICorrelationIdAccessor correlationIdAccessor) : IPortalProductClient
{
    private static readonly JsonSerializerOptions PatchSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<PortalProductListResult> ListProductsAsync(
        PortalProductListQuery query,
        CancellationToken cancellationToken)
    {
        var parameters = new List<string>
        {
            $"page={query.Page}",
            $"size={query.PageSize}",
        };

        AddQueryParameter(parameters, "keywords", query.Keywords);
        AddQueryParameter(parameters, "sorting", query.Sorting);

        using var response = await SendAsync(HttpMethod.Get, $"store/products?{string.Join('&', parameters)}", null, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccessfulPortalResponse(response, json);

        try
        {
            return PortalProductMapper.MapList(json, query);
        }
        catch (JsonException exception)
        {
            throw new PortalUnavailableException(exception);
        }
    }

    public async Task<PortalProductDetail?> GetProductAsync(
        string externalProductId,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"store/products/{Uri.EscapeDataString(externalProductId)}",
            null,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccessfulPortalResponse(response, json);

        try
        {
            return PortalProductMapper.MapDetail(json);
        }
        catch (JsonException exception)
        {
            throw new PortalUnavailableException(exception);
        }
    }

    public async Task<PortalProductVariant?> GetVariantAsync(
        string externalVariantId,
        CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"store/products/variants/{Uri.EscapeDataString(externalVariantId)}",
            null,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccessfulPortalResponse(response, json);

        try
        {
            return PortalProductMapper.MapVariant(json);
        }
        catch (JsonException exception)
        {
            throw new PortalUnavailableException(exception);
        }
    }

    public async Task UpdateVariantAsync(
        string externalVariantId,
        PortalVariantPatch patch,
        CancellationToken cancellationToken)
    {
        using var content = JsonContent.Create(patch, options: PatchSerializerOptions);
        using var response = await SendAsync(
            HttpMethod.Patch,
            $"store/products/variants/{Uri.EscapeDataString(externalVariantId)}",
            content,
            cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccessfulPortalResponse(response, json);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativeUrl,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, relativeUrl) { Content = content };
        var token = await tokenProvider.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationIdAccessor.CorrelationId);

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

    private static void EnsureSuccessfulPortalResponse(HttpResponseMessage response, string json)
    {
        if (!response.IsSuccessStatusCode || IsExplicitPortalFailure(json))
        {
            throw new PortalUnavailableException();
        }
    }

    private static bool IsExplicitPortalFailure(string json)
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

    private static void AddQueryParameter(List<string> parameters, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters.Add($"{name}={Uri.EscapeDataString(value)}");
        }
    }
}
