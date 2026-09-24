using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

/// <summary>
/// No email/SMS infrastructure exists in this project (ADR-023); "announcing
/// readiness" for a >25,000-row export means exactly these status/download
/// endpoints for the client to poll — not a real notification. Noted as an
/// interpretive decision, not a literal prompt requirement.
/// </summary>
internal static class GetExportStatusEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/audit/export/{jobId:guid}/status", async Task<Results<Ok<AuditExportStatusDto>, NotFound>> (
                Guid jobId,
                AuditingDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var job = await dbContext.AuditExportJobs.AsNoTracking().SingleOrDefaultAsync(j => j.Id == jobId, cancellationToken);
                if (job is null)
                {
                    return TypedResults.NotFound();
                }

                return TypedResults.Ok(new AuditExportStatusDto(
                    job.Id,
                    job.Status.ToString(),
                    job.Format,
                    job.ErrorMessage,
                    job.CreatedAt,
                    job.CompletedAt));
            })
            .RequireAuthorization()
            .RequirePermission(AuditingPermissions.Export);
    }
}

internal sealed record AuditExportStatusDto(
    Guid Id,
    string Status,
    string Format,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
