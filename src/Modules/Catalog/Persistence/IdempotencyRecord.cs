namespace NafasLand.Admin.Modules.Catalog.Persistence;

internal sealed class IdempotencyRecord
{
    private IdempotencyRecord()
    {
        RequestHash = string.Empty;
    }

    public Guid Key { get; private set; }

    public Guid UserId { get; private set; }

    public string RequestHash { get; private set; }

    public IdempotencyRecordStatus Status { get; private set; }

    public string? ResponseJson { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static IdempotencyRecord Start(Guid key, Guid userId, string requestHash, DateTimeOffset createdAt)
    {
        return new IdempotencyRecord
        {
            Key = key,
            UserId = userId,
            RequestHash = requestHash,
            Status = IdempotencyRecordStatus.InProgress,
            CreatedAt = createdAt,
        };
    }

    public void Complete(string responseJson)
    {
        Status = IdempotencyRecordStatus.Completed;
        ResponseJson = responseJson;
    }
}
