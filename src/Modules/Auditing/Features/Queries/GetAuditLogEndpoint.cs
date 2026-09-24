using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Kernel.Users;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

/// <summary>
/// ADR-014, view 3 (record detail). BeforeJson/AfterJson/ChangedFields are
/// returned already parsed, not as raw JSON strings, so the frontend renders
/// "before -> after" without a second parse step — and with sensitive keys
/// already masked server-side (SensitiveAuditFields).
/// </summary>
internal static class GetAuditLogEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/audit/logs/{id:guid}", async Task<Results<Ok<AuditLogDetailDto>, NotFound>> (
                Guid id,
                AuditingDbContext dbContext,
                IUserDirectory userDirectory,
                CancellationToken cancellationToken) =>
            {
                var log = await dbContext.AuditLogs.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
                if (log is null)
                {
                    return TypedResults.NotFound();
                }

                var userIds = new[] { log.ActorUserId, log.OnBehalfOfUserId }.OfType<Guid>().ToList();
                var usernames = await userDirectory.GetUsernamesAsync(userIds, cancellationToken);

                return TypedResults.Ok(new AuditLogDetailDto(
                    log.Id,
                    log.CorrelationId,
                    log.ActorUserId,
                    AuditLogSummaryDto.UsernameOf(log.ActorUserId, usernames),
                    log.OnBehalfOfUserId,
                    AuditLogSummaryDto.UsernameOf(log.OnBehalfOfUserId, usernames),
                    log.ActorRoleAtTime,
                    log.Action,
                    log.EntityType,
                    log.EntityId,
                    log.ParentEntityType,
                    log.ParentEntityId,
                    SensitiveAuditFields.ParseAndMask(log.BeforeJson),
                    SensitiveAuditFields.ParseAndMask(log.AfterJson),
                    SensitiveAuditFields.ParseAndMask(log.ChangedFields),
                    log.Outcome.ToString(),
                    log.UpstreamStatus,
                    log.FailureReason,
                    log.IpAddress,
                    log.UserAgent,
                    log.CreatedAt));
            })
            .RequireAuthorization()
            .RequirePermission(AuditingPermissions.ReadAll);
    }
}
