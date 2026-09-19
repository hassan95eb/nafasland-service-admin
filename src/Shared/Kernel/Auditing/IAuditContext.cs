namespace NafasLand.Admin.Shared.Kernel.Auditing;

/// <summary>
/// Filled by a command's own Handler during execution, so the handler never
/// writes to AuditLog directly (ADR-009, ADR-048) — only AuditBehavior does,
/// after the handler returns. Scoped per request; only meaningful for a command
/// implementing <see cref="IAuditableCommand"/>, though any handler may call it
/// safely (a handler for a non-auditable command doing so has no effect, since
/// nothing ever reads it in that case).
/// </summary>
public interface IAuditContext
{
    void SetEntityId(string entityId);

    /// <summary>Serialized with System.Text.Json. Omit entirely for a Create operation, which has no "before".</summary>
    void SetBefore(object? snapshot);

    void SetAfter(object? snapshot);
}
