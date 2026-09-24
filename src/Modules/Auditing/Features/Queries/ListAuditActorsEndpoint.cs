using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Kernel.Users;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

/// <summary>
/// Options for the global log's user filter: everyone who appears as an actor
/// in the log, under audit.read.all itself — so the report page never depends on
/// identity.users.manage (which a SuperAdmin could in principle lack), and a user
/// no longer in Identity still shows up to be filtered by.
/// </summary>
internal static class ListAuditActorsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/audit/actors", async (
                AuditingDbContext dbContext,
                IUserDirectory userDirectory,
                CancellationToken cancellationToken) =>
            {
                var actorIds = await dbContext.AuditLogs
                    .AsNoTracking()
                    .Where(log => log.ActorUserId != null)
                    .Select(log => log.ActorUserId!.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var usernames = await userDirectory.GetUsernamesAsync(actorIds, cancellationToken);
                var actors = actorIds
                    .Select(id => new AuditActorDto(id, AuditLogSummaryDto.UsernameOf(id, usernames)))
                    .OrderBy(actor => actor.Username is null)
                    .ThenBy(actor => actor.Username, StringComparer.Ordinal)
                    .ToList();

                return TypedResults.Ok(actors);
            })
            .RequireAuthorization()
            .RequirePermission(AuditingPermissions.ReadAll);
    }
}
