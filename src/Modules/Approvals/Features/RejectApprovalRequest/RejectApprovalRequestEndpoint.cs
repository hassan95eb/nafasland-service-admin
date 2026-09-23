using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Approvals.Features.RejectApprovalRequest;

internal static class RejectApprovalRequestEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/approvals/{id:guid}/rejection", async (
                Guid id,
                RejectApprovalRequestRequest request,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var command = new RejectApprovalRequestCommand(id, request.Note);
                var result = await dispatcher.SendAsync<RejectApprovalRequestCommand, ApprovalDecisionResult>(command, cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization();
    }
}

internal sealed record RejectApprovalRequestRequest(string Note);
