using System.Text.Json;
using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Shared.Infrastructure.Auditing;

/// <summary>
/// Scoped implementation of IAuditContext. Registered twice in DI (as itself and
/// as IAuditContext, pointing at the same instance) so a Handler writes through
/// the narrow Kernel interface while AuditBehavior — in the same assembly — reads
/// the accumulated EntityId/BeforeJson/AfterJson/ChangedFields back off the
/// concrete type; IAuditContext itself deliberately exposes no getters; that
/// widening is Shared.Infrastructure's own business, not something a Handler
/// (or any other module) should see.
/// </summary>
internal sealed class AuditContext : IAuditContext
{
    public string? EntityId { get; private set; }

    public string? BeforeJson { get; private set; }

    public string? AfterJson { get; private set; }

    public void SetEntityId(string entityId) => EntityId = entityId;

    public void SetBefore(object? snapshot) => BeforeJson = Serialize(snapshot);

    public void SetAfter(object? snapshot) => AfterJson = Serialize(snapshot);

    /// <summary>
    /// Top-level field names whose JSON representation differs between Before and
    /// After (ADR-048) — a key present in only one of them counts as changed too.
    /// Empty when either snapshot is missing (nothing to compare, not an error).
    /// </summary>
    public IReadOnlyList<string> ComputeChangedFields()
    {
        if (BeforeJson is null || AfterJson is null)
        {
            return [];
        }

        using var before = JsonDocument.Parse(BeforeJson);
        using var after = JsonDocument.Parse(AfterJson);

        var beforeFields = before.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetRawText());
        var afterFields = after.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetRawText());

        var changed = new List<string>();
        foreach (var name in beforeFields.Keys.Union(afterFields.Keys))
        {
            var hasBefore = beforeFields.TryGetValue(name, out var beforeValue);
            var hasAfter = afterFields.TryGetValue(name, out var afterValue);
            if (!hasBefore || !hasAfter || beforeValue != afterValue)
            {
                changed.Add(name);
            }
        }

        return changed;
    }

    private static string? Serialize(object? snapshot) => snapshot is null ? null : JsonSerializer.Serialize(snapshot);
}
