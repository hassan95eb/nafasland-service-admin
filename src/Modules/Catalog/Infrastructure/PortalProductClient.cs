using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Shared.Infrastructure.Portal;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Infrastructure;

internal sealed class PortalProductClient(HttpClient httpClient) : IPortalProductClient
{
    private static readonly JsonSerializerOptions PatchSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions ProductSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
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

    public async Task<PortalProductCreateResult> CreateProductAsync(
        PortalProductWriteModel product,
        CancellationToken cancellationToken)
    {
        using var content = JsonContent.Create(product, options: ProductSerializerOptions);
        using var response = await SendAsync(HttpMethod.Post, "store/products", content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccessfulPortalResponse(response, json);

        try
        {
            return PortalProductMapper.MapCreateResult(json);
        }
        catch (JsonException exception)
        {
            throw new PortalUnavailableException(exception);
        }
    }

    public async Task<PortalProductUpdateResult> UpdateProductAsync(
        string externalProductId,
        PortalProductWriteModel product,
        CancellationToken cancellationToken)
    {
        using var content = JsonContent.Create(product, options: ProductSerializerOptions);
        using var response = await SendAsync(
            HttpMethod.Put,
            $"store/products/{Uri.EscapeDataString(externalProductId)}",
            content,
            cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccessfulPortalResponse(response, json);

        try
        {
            return PortalProductMapper.MapUpdateResult(json);
        }
        catch (JsonException exception)
        {
            throw new PortalUnavailableException(exception);
        }
    }

    public async Task DeleteProductAsync(string externalProductId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            HttpMethod.Delete,
            $"store/products/{Uri.EscapeDataString(externalProductId)}",
            null,
            cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccessfulPortalResponse(response, json);
    }

    public async Task<IReadOnlyList<PortalCategoryNode>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, "../pages", null, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccessfulPortalResponse(response, json);
        try
        {
            return PortalProductMapper.MapCategories(json);
        }
        catch (JsonException exception)
        {
            throw new PortalUnavailableException(exception);
        }
    }

    public async Task<IReadOnlyList<PortalFilterGroup>> ListFiltersAsync(CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, "../store/filters?relation=", null, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccessfulPortalResponse(response, json);
        try
        {
            return PortalProductMapper.MapFilters(json);
        }
        catch (JsonException exception)
        {
            throw new PortalUnavailableException(exception);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string relativeUrl,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, relativeUrl) { Content = content };
        return await PortalHttp.SendAsync(httpClient, request, cancellationToken);
    }

    private static void EnsureSuccessfulPortalResponse(HttpResponseMessage response, string json) =>
        PortalHttp.EnsureSuccess(response, json);

    private static void AddQueryParameter(List<string> parameters, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters.Add($"{name}={Uri.EscapeDataString(value)}");
        }
    }
}
