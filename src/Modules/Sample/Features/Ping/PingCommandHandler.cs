using NafasLand.Admin.Modules.Sample.Persistence;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Sample.Features.Ping;

/// <summary>
/// Only adds the record to the ChangeTracker; SaveChanges is called by
/// TransactionBehavior when the transaction commits. The IAuditContext calls are
/// step 2's optional proof-of-mechanism (see PingCommand's own comment) — the
/// handler still never writes to AuditLog itself, only AuditBehavior does.
/// </summary>
internal sealed class PingCommandHandler(SampleDbContext dbContext, TimeProvider timeProvider, IAuditContext auditContext)
    : ICommandHandler<PingCommand, PingResult>
{
    public Task<PingResult> HandleAsync(PingCommand command, CancellationToken cancellationToken)
    {
        var record = new PingRecord(command.Message, timeProvider.GetUtcNow().UtcDateTime);
        dbContext.PingRecords.Add(record);

        auditContext.SetEntityId(record.Id.ToString());
        auditContext.SetAfter(new { record.Message, record.CreatedAtUtc });

        return Task.FromResult(new PingResult(record.Id, record.Message, record.CreatedAtUtc));
    }
}
