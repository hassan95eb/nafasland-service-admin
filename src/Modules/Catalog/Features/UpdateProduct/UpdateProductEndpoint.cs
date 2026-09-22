using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Catalog.Features.UpdateProduct;

internal static class UpdateProductEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/v1/catalog/products/{productId}", async (
                string productId,
                UpdateProductRequest request,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var result = await dispatcher.SendAsync<UpdateProductCommand, UpdateProductResult>(
                    new UpdateProductCommand(
                        productId,
                        request.LastKnownVersion,
                        request.Title,
                        request.Caption,
                        request.Description,
                        request.DescriptionDirty,
                        request.Contents,
                        request.CommentingEnabled,
                        request.Fields,
                        request.Slug,
                        request.MetaTitle,
                        request.MetaDescription,
                        request.MetaKeywords,
                        request.MetaRobots,
                        request.Redirect,
                        request.CategoryIds,
                        request.FilterIds),
                    cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization();
    }
}

internal sealed record UpdateProductRequest(
    string LastKnownVersion,
    string Title,
    string? Caption,
    string? Description,
    bool DescriptionDirty,
    IReadOnlyList<UpdateProductContentInput> Contents,
    bool CommentingEnabled,
    IReadOnlyList<PortalNameValue> Fields,
    string? Slug,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? MetaRobots,
    string? Redirect,
    IReadOnlyList<long> CategoryIds,
    IReadOnlyList<long> FilterIds);
