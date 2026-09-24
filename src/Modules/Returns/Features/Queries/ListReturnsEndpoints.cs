using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Returns.Persistence;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Users;

namespace NafasLand.Admin.Modules.Returns.Features.Queries;

/// <summary>
/// Approved returns (read-only queries, outside the command pipeline like
/// Auditing's). Without returns.read.all a user only ever sees records they
/// registered; someone else's record answers 404, not 403, so its existence
/// is not revealed.
/// </summary>
internal static class ListReturnsEndpoints
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/returns", async (
                HttpContext httpContext,
                string? cursor,
                int? pageSize,
                ReturnsDbContext dbContext,
                IUserDirectory userDirectory,
                CancellationToken cancellationToken) =>
            {
                var effectivePageSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);

                var rows = await dbContext.ReturnRecords
                    .AsNoTracking()
                    .VisibleTo(httpContext.User)
                    .ApplyKeysetCursor(cursor)
                    .OrderByDescending(record => record.ApprovedAt)
                    .ThenByDescending(record => record.Id)
                    .Take(effectivePageSize + 1)
                    .ToListAsync(cancellationToken);

                return TypedResults.Ok(await ReturnRecordPageDto.CreateAsync(rows, effectivePageSize, userDirectory, cancellationToken));
            })
            .RequireAuthorization();

        app.MapGet("/api/v1/returns/{id:guid}", async (
                Guid id,
                HttpContext httpContext,
                ReturnsDbContext dbContext,
                IUserDirectory userDirectory,
                CancellationToken cancellationToken) =>
            {
                var record = await dbContext.ReturnRecords
                    .AsNoTracking()
                    .VisibleTo(httpContext.User)
                    .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
                    ?? throw new ResourceNotFoundException("مرجوعی پیدا نشد.");

                var usernames = await userDirectory.GetUsernamesAsync(
                    [record.RegisteredByUserId, record.ApprovedByUserId],
                    cancellationToken);

                return TypedResults.Ok(ReturnRecordDetailDto.FromEntity(record, usernames));
            })
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
