using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Approvals.Features.RetryApprovalRequest;

internal static class RetryApprovalRequestEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/approvals/{id:guid}/retry", async (
                Guid id,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var command = new RetryApprovalRequestCommand(id);
                var result = await dispatcher.SendAsync<RetryApprovalRequestCommand, ApprovalDecisionResult>(command, cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization();
    }
}
