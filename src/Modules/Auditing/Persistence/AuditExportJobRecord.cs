namespace NafasLand.Admin.Modules.Auditing.Persistence;

/// <summary>
/// Not part of the prompt's own data model (only AuditLog/ProductRef are named
/// there); added because ">25,000 rows" mode explicitly needs a jobId that
/// GET .../export/{jobId}/status and .../download can look up later, and
/// Hangfire's own job id is an internal storage detail this module should not
/// leak through its own API. See the step-02 report for this as a flagged
/// assumption, not a literal prompt requirement.
/// </summary>
internal enum AuditExportJobStatus
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
}

internal sealed class AuditExportJobRecord
{
    private AuditExportJobRecord()
    {
        Format = string.Empty;
    }

    public Guid Id { get; private set; }

    public AuditExportJobStatus Status { get; private set; }

    /// <summary>"csv" or "xlsx".</summary>
    public string Format { get; private set; }

    public string? FilePath { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static AuditExportJobRecord CreateQueued(string format, DateTimeOffset now)
    {
        return new AuditExportJobRecord
        {
            Id = Guid.NewGuid(),
            Status = AuditExportJobStatus.Queued,
            Format = format,
            CreatedAt = now,
        };
    }

    public void MarkProcessing() => Status = AuditExportJobStatus.Processing;

    public void MarkCompleted(string filePath, DateTimeOffset now)
    {
        Status = AuditExportJobStatus.Completed;
        FilePath = filePath;
        CompletedAt = now;
    }

    public void MarkFailed(string errorMessage, DateTimeOffset now)
    {
        Status = AuditExportJobStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = now;
    }
}
