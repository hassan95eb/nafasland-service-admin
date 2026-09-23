using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;

internal static class ApproveApprovalRequestEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/approvals/{id:guid}/approval", async (
                Guid id,
                ApproveApprovalRequestRequest? request,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var command = new ApproveApprovalRequestCommand(id, request?.Note);
                var result = await dispatcher.SendAsync<ApproveApprovalRequestCommand, ApprovalDecisionResult>(command, cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization();
    }
}

internal sealed record ApproveApprovalRequestRequest(string? Note);
