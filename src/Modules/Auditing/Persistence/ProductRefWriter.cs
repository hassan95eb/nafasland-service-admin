using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Auditing.Persistence;

/// <summary>
/// Shares the scoped AuditingDbContext with AuditLogWriter, which saves right
/// after this within the same request. A failed save here must therefore not
/// leave its ProductRef tracked, or AuditLogWriter's own SaveChanges would retry
/// it and lose the AuditLog row too — hence the detach on failure. ProductRef is
/// display metadata; the next successful write of the same product refreshes it.
/// </summary>
internal sealed class ProductRefWriter(
    AuditingDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ProductRefWriter> logger) : IProductRefWriter
{
    private const int MaxTitleLength = 500;

    public async Task UpsertAsync(string externalProductId, string title, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var trimmedTitle = title.Length > MaxTitleLength ? title[..MaxTitleLength] : title;

        ProductRef? productRef = null;
        try
        {
            productRef = await dbContext.ProductRefs.SingleOrDefaultAsync(p => p.ExternalProductId == externalProductId, cancellationToken);
            if (productRef is null)
            {
                productRef = ProductRef.Create(externalProductId, trimmedTitle, now);
                dbContext.ProductRefs.Add(productRef);
            }
            else
            {
                productRef.Refresh(trimmedTitle, now);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            if (productRef is not null)
            {
                dbContext.Entry(productRef).State = EntityState.Detached;
            }

            logger.LogWarning(exception, "به‌روزرسانی ProductRef برای محصول {ExternalProductId} انجام نشد.", externalProductId);
        }
    }
}
