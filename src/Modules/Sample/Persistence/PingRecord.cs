namespace NafasLand.Admin.Modules.Sample.Persistence;

/// <summary>
/// رکوردی که با هر اجرای موفق PingCommand نوشته می‌شود تا تراکنش و مهاجرت
/// این ماژول واقعاً امتحان شوند (هدف گام ۰).
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
