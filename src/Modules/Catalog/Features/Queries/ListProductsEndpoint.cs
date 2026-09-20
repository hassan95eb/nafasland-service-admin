using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Catalog.Features.Queries;

internal static class ListProductsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/catalog/products", HandleAsync)
            .RequireAuthorization()
            .RequirePermission(CatalogPermissions.ProductsRead);
    }

    internal static async Task<IResult> HandleAsync(
        int page,
        int pageSize,
        string? keywords,
        string? sorting,
        ProductListCache cache,
        IPortalProductClient client,
        CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["pagination"] = ["page باید حداقل ۱ و pageSize باید بین ۱ تا ۱۰۰ باشد."],
            });
        }

        var query = new PortalProductListQuery(page, pageSize, keywords, sorting);
        var result = await cache.GetOrCreateAsync(
            query,
            token => client.ListProductsAsync(query, token),
            cancellationToken);

        return Results.Ok(result);
    }
}
