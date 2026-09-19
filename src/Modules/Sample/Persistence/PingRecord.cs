namespace NafasLand.Admin.Modules.Sample.Persistence;

/// <summary>
/// A record written on every successful PingCommand execution, so this module's
/// transaction and migration are actually exercised (step 0's goal).
/// </summary>
internal sealed class PingRecord
{
    private PingRecord()
    {
        Message = string.Empty;
    }

    public PingRecord(string message, DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        Message = message;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Message { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
