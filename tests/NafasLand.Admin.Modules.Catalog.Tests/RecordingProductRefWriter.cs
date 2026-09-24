using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Catalog.Tests;

internal sealed class RecordingProductRefWriter : IProductRefWriter
{
    public List<(string ExternalProductId, string Title)> Upserts { get; } = [];

    public Task UpsertAsync(string externalProductId, string title, CancellationToken cancellationToken)
    {
        Upserts.Add((externalProductId, title));
        return Task.CompletedTask;
    }
}
