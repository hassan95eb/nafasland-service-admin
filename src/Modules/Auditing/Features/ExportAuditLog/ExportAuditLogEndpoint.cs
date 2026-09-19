using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NafasLand.Admin.Modules.Auditing.Features.Queries;
using NafasLand.Admin.Shared.Kernel.Auditing;
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
                    request.Outcome);

                var command = new ExportAuditLogCommand(filter, request.Format);
                var result = await dispatcher.SendAsync<ExportAuditLogCommand, ExportAuditLogResult>(command, cancellationToken);

                return result.IsAsync
                    ? Results.Accepted($"/api/v1/audit/export/{result.JobId}/status", new { jobId = result.JobId })
                    : Results.File(result.FileBytes!, result.ContentType!, result.FileName);
            })
            .RequireAuthorization();
    }
}

internal sealed record ExportAuditLogRequest(
    Guid? ActorUserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Action,
    string? EntityType,
    AuditOutcome? Outcome,
    string Format);
