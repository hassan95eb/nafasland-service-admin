using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Auditing.Persistence;

/// <summary>
/// The only place a row is actually inserted into AuditLog (ADR-009, ADR-048).
/// Registered without a key (unlike the per-module keyed IUnitOfWork) because
/// there is exactly one AuditLog table for the whole application.
/// </summary>
internal sealed class AuditLogWriter(AuditingDbContext dbContext, TimeProvider timeProvider) : IAuditLogWriter
{
    public async Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        var log = AuditLog.FromEntry(entry, timeProvider.GetUtcNow().UtcDateTime);
        dbContext.AuditLogs.Add(log);

        // AuditBehavior/AuthorizationBehavior call this from inside a command
        // whose own transaction is committed by TransactionBehavior — but a
        // Denied write happens BEFORE that commit ever runs (the command throws
        // first), and a background job (purge, export) has no ambient
        // transaction at all. Saving immediately here means the audit row survives
        // in both cases instead of depending on someone else's commit.
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
