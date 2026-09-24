using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Returns.Approvals;
using NafasLand.Admin.Modules.Returns.Infrastructure;
using NafasLand.Admin.Modules.Returns.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Returns.Features.Queries;

/// <summary>
/// Reads one order live from the portal for the return form (read-only, ADR-054).
/// Portal failures surface through the shared ProblemDetails handler with a
/// correlationId and never with the portal's own message (ADR-036).
/// </summary>
internal static class GetOrderPreviewEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/returns/orders/{orderId}", async (
                string orderId,
                IPortalOrderClient portalClient,
                ReturnsDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                // The browser normalizes Persian/Arabic digits in its shared
                // normalizer (rule 14); here only a plain positive integer passes.
                if (!long.TryParse(orderId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
                {
                    throw new CommandValidationException(new Dictionary<string, string[]>
                    {
                        ["orderId"] = ["شمارهٔ سفارش باید یک عدد صحیح مثبت باشد."],
                    });
                }

                var order = await portalClient.GetOrderAsync(id, cancellationToken)
                    ?? throw new ResourceNotFoundException(ReturnRules.OrderNotFound);

                var ineligibilityReason = ReturnRules.CheckOrderEligibility(order);
                if (ineligibilityReason is null
                    && await dbContext.ReturnRecords.AnyAsync(record => record.OrderId == order.OrderId, cancellationToken))
                {
                    ineligibilityReason = ReturnRules.AlreadyRegistered;
                }

                return TypedResults.Ok(OrderPreviewDto.FromOrder(order, ineligibilityReason));
            })
            .RequireAuthorization()
            .RequirePermission(ReturnsPermissions.Request)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
