using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

internal static class DownloadExportEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/audit/export/{jobId:guid}/download", async (
                Guid jobId,
                AuditingDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var job = await dbContext.AuditExportJobs.AsNoTracking().SingleOrDefaultAsync(j => j.Id == jobId, cancellationToken);
                if (job is null)
                {
                    return Results.NotFound();
                }

                if (job.Status != AuditExportJobStatus.Completed || job.FilePath is null || !File.Exists(job.FilePath))
                {
                    return Results.Conflict(new { message = "فایل هنوز آماده نیست." });
                }

                var contentType = job.Format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "text/csv";

                var bytes = await File.ReadAllBytesAsync(job.FilePath, cancellationToken);
                return Results.File(bytes, contentType, Path.GetFileName(job.FilePath));
            })
            .RequireAuthorization()
            .RequirePermission(AuditingPermissions.Export);
    }
}
