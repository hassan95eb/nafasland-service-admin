using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Catalog.Features.Queries;

internal static class GetProductTaxonomyEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/catalog/categories", async (
                TaxonomyCache cache,
                IPortalProductClient client,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await cache.GetCategoriesAsync(client.ListCategoriesAsync, cancellationToken)))
            .RequireAuthorization()
            .RequirePermission(CatalogPermissions.ProductsWrite);

        app.MapGet("/api/v1/catalog/filters", async (
                TaxonomyCache cache,
                IPortalProductClient client,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(await cache.GetFiltersAsync(client.ListFiltersAsync, cancellationToken)))
            .RequireAuthorization()
            .RequirePermission(CatalogPermissions.ProductsWrite);
    }
}
