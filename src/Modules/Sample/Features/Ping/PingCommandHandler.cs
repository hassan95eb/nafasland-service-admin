using NafasLand.Admin.Modules.Sample.Persistence;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Sample.Features.Ping;

/// <summary>
/// Only adds the record to the ChangeTracker; SaveChanges is called by
/// TransactionBehavior when the transaction commits.
/// </summary>
internal sealed class PingCommandHandler(SampleDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<PingCommand, PingResult>
{
    public Task<PingResult> HandleAsync(PingCommand command, CancellationToken cancellationToken)
    {
        var record = new PingRecord(command.Message, timeProvider.GetUtcNow().UtcDateTime);
        dbContext.PingRecords.Add(record);

        return Task.FromResult(new PingResult(record.Id, record.Message, record.CreatedAtUtc));
    }
}
