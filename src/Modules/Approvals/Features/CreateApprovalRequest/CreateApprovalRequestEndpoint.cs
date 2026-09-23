using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Approvals.Features.CreateApprovalRequest;

internal static class CreateApprovalRequestEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/approvals", async (
                CreateApprovalRequestRequest request,
                IApprovalExecutorRegistry registry,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var executor = registry.Resolve(request.RequestType)
                    ?? throw new BusinessRuleException("نوع درخواست تأیید نامعتبر است.");

                var command = new CreateApprovalRequestCommand(
                    request.RequestType,
                    request.TargetEntityType,
                    request.TargetEntityId,
                    request.Reason,
                    request.Payload.GetRawText(),
                    executor.RequestPermission);

                var result = await dispatcher.SendAsync<CreateApprovalRequestCommand, CreateApprovalRequestResult>(command, cancellationToken);
                return TypedResults.Ok(result);
            })
            .RequireAuthorization();
    }
}

/// <summary>
/// Payload is an opaque JSON subtree here — never deserialized polymorphically by
/// an embedded type name (ADR-010, rule 1). Once RequestType resolves an
/// executor, that executor alone knows the concrete payload shape it expects and
/// deserializes it itself (see e.g. Catalog's DeleteProductApprovalExecutor).
/// </summary>
internal sealed record CreateApprovalRequestRequest(
    string RequestType,
    string TargetEntityType,
    string TargetEntityId,
    string Reason,
    JsonElement Payload);
