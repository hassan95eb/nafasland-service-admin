using NafasLand.Admin.Modules.Sample.Persistence;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Sample.Features.Ping;

/// <summary>
/// فقط رکورد را به ChangeTracker اضافه می‌کند؛ SaveChanges توسط
/// TransactionBehavior در commit تراکنش صدا زده می‌شود.
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
