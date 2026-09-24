using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;

namespace NafasLand.Admin.Modules.Catalog.Features.Queries;

internal static class GetProductEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/catalog/products/{externalProductId}", HandleAsync)
            .RequireAuthorization()
            .RequirePermission(CatalogPermissions.ProductsRead);
    }

    internal static async Task<Results<Ok<PortalProductDetail>, ProblemHttpResult>> HandleAsync(
        string externalProductId,
        ProductCache cache,
        IPortalProductClient client,
        ICorrelationIdAccessor correlationIdAccessor,
        CancellationToken cancellationToken)
    {
        var product = await cache.GetDetailAsync(
            externalProductId,
            token => client.GetProductAsync(externalProductId, token),
            cancellationToken);
        if (product is not null)
        {
            return TypedResults.Ok(product);
        }

        return TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "محصول پیدا نشد",
            detail: "محصول در نفس‌لند پیدا نشد.",
            extensions: new Dictionary<string, object?>
            {
                ["correlationId"] = correlationIdAccessor.CorrelationId,
            });
    }
}
