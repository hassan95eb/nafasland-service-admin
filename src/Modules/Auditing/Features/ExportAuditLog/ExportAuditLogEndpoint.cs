using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Modules.Auditing.Features.Queries;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Auditing.Features.ExportAuditLog;

internal static class ExportAuditLogEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/audit/export", async (
                ExportAuditLogRequest request,
                ICommandDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var filter = new AuditLogFilter(
                    request.ActorUserId,
                    request.From,
                    request.To,
                    request.Action,
                    request.EntityType,
                    ParseOutcome(request.Outcome));

                var command = new ExportAuditLogCommand(filter, request.Format);
                var result = await dispatcher.SendAsync<ExportAuditLogCommand, ExportAuditLogResult>(command, cancellationToken);

                return result.IsAsync
                    ? Results.Accepted($"/api/v1/audit/export/{result.JobId}/status", new AuditExportAcceptedDto(result.JobId!.Value))
                    : Results.File(result.FileBytes!, result.ContentType!, result.FileName);
            })
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .Produces<AuditExportAcceptedDto>(StatusCodes.Status202Accepted)
            .RequireAuthorization();
    }

    private static AuditOutcome? ParseOutcome(string? outcome)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            return null;
        }

        return Enum.GetNames<AuditOutcome>().Contains(outcome)
            ? Enum.Parse<AuditOutcome>(outcome)
            : throw new CommandValidationException(new Dictionary<string, string[]>
            {
                ["outcome"] = ["outcome باید Success، Failed یا Denied باشد."],
            });
    }
}

/// <summary>
/// Outcome is the name ("Success"/"Failed"/"Denied") — exactly what the list
/// endpoint's query string takes — rather than the enum, which OpenAPI would
/// publish as a bare number for the generated frontend types (ADR-051).
/// </summary>
internal sealed record ExportAuditLogRequest(
    Guid? ActorUserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Action,
    string? EntityType,
    string? Outcome,
    string Format);

internal sealed record AuditExportAcceptedDto(Guid JobId);
