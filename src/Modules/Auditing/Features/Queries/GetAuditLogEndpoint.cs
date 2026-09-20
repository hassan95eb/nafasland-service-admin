using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

/// <summary>
/// ADR-014, view 3 (record detail). BeforeJson/AfterJson/ChangedFields are
/// returned already deserialized (Dictionary/array), not as raw JSON strings —
/// simpler for the step-4 frontend to render "before -> after" without a second
/// parse step, and there is no reason to keep them opaque to this API's own caller.
/// </summary>
internal static class GetAuditLogEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/audit/logs/{id:guid}", async (
                Guid id,
                AuditingDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var log = await dbContext.AuditLogs.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
                if (log is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(new
                {
                    log.Id,
                    log.CorrelationId,
                    log.ActorUserId,
                    log.OnBehalfOfUserId,
                    log.ActorRoleAtTime,
                    log.Action,
                    log.EntityType,
                    log.EntityId,
                    Before = Deserialize(log.BeforeJson),
                    After = Deserialize(log.AfterJson),
                    ChangedFields = Deserialize(log.ChangedFields),
                    Outcome = log.Outcome.ToString(),
                    log.UpstreamStatus,
                    log.FailureReason,
                    log.IpAddress,
                    log.UserAgent,
                    log.CreatedAt,
                });
            })
            .RequireAuthorization()
            .RequirePermission(AuditingPermissions.ReadAll);
    }

    private static object? Deserialize(string? json) =>
        json is null ? null : JsonSerializer.Deserialize<JsonElement>(json);
}
