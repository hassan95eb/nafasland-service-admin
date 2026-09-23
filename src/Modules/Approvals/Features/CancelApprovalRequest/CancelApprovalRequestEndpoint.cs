using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Approvals.Features.CancelApprovalRequest;

internal static class CancelApprovalRequestEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/approvals/{id:guid}/cancellation", async (
                Guid id,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var command = new CancelApprovalRequestCommand(id);
                var result = await dispatcher.SendAsync<CancelApprovalRequestCommand, ApprovalDecisionResult>(command, cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization();
    }
}
