using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Shared.Infrastructure.Auditing;

/// <summary>Same role as NullAuditLogWriter: lets Catalog resolve when the Auditing module is disabled or absent (tests).</summary>
internal sealed class NullProductRefWriter : IProductRefWriter
{
    public Task UpsertAsync(string externalProductId, string title, CancellationToken cancellationToken) => Task.CompletedTask;
}
