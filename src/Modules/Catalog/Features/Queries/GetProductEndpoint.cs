using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Modules.Catalog.Contracts;
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

    internal static async Task<IResult> HandleAsync(
        string externalProductId,
        IPortalProductClient client,
        ICorrelationIdAccessor correlationIdAccessor,
        CancellationToken cancellationToken)
    {
        var product = await client.GetProductAsync(externalProductId, cancellationToken);
        if (product is not null)
        {
            return Results.Ok(product);
        }

        return Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "محصول پیدا نشد",
            detail: "محصول در پرتال پیدا نشد.",
            extensions: new Dictionary<string, object?>
            {
                ["correlationId"] = correlationIdAccessor.CorrelationId,
            });
    }
}
