using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Catalog.Features.UpdateVariantPriceAndInventory;

internal static class UpdateVariantPriceAndInventoryEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/v1/catalog/products/variants/{variantId}", async (
                string variantId,
                UpdateVariantPriceAndInventoryRequest request,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateVariantPriceAndInventoryCommand(
                    variantId,
                    request.NewPrice,
                    request.NewStock,
                    request.LastKnownPrice,
                    request.LastKnownStock);
                var result = await dispatcher.SendAsync<
                    UpdateVariantPriceAndInventoryCommand,
                    UpdateVariantPriceAndInventoryResult>(command, cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization();
    }
}

internal sealed record UpdateVariantPriceAndInventoryRequest(
    decimal NewPrice,
    int NewStock,
    decimal? LastKnownPrice,
    int? LastKnownStock);
