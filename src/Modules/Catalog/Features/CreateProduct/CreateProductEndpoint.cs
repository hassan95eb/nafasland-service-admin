using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Catalog.Features.CreateProduct;

internal static class CreateProductEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/catalog/products", async (
                CreateProductRequest request,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var result = await dispatcher.SendAsync<CreateProductCommand, CreateProductResult>(
                    new CreateProductCommand(
                        request.Title,
                        request.Caption,
                        request.Description,
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
                        request.FilterIds,
                        request.Attributes,
                        request.Variants),
                    cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization();
    }
}

internal sealed record CreateProductRequest(
    string Title,
    string? Caption,
    string? Description,
    IReadOnlyList<PortalNameValue> Contents,
    bool CommentingEnabled,
    IReadOnlyList<PortalNameValue> Fields,
    string? Slug,
    string? MetaTitle,
    string? MetaDescription,
    string? MetaKeywords,
    string? MetaRobots,
    string? Redirect,
    IReadOnlyList<long> CategoryIds,
    IReadOnlyList<long> FilterIds,
    IReadOnlyList<PortalAttribute> Attributes,
    IReadOnlyList<CreateProductVariantInput> Variants);
