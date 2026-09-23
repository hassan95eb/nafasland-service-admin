using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Approvals.Persistence;

/// <summary>
/// ADR-010's entity, exactly as specified. A decision is one-time and terminal
/// (ADR-010, rule 3): every transition out of <see cref="ApprovalRequestStatus.Pending"/>
/// is guarded by <see cref="EnsurePending"/>, and concurrent decisions on the same
/// row are additionally caught by <see cref="RowVersion"/> at SaveChanges time
/// (EF's native SQL Server rowversion concurrency token — see ApprovalRequestConfiguration).
/// </summary>
internal sealed class ApprovalRequest
{
    private ApprovalRequest()
    {
        RequestType = string.Empty;
        TargetEntityType = string.Empty;
        TargetEntityId = string.Empty;
        PayloadJson = string.Empty;
        Reason = string.Empty;
        RowVersion = [];
    }

    public Guid Id { get; private set; }

    public string RequestType { get; private set; }

    public string TargetEntityType { get; private set; }

    public string TargetEntityId { get; private set; }

    public string PayloadJson { get; private set; }

    /// <summary>A best-effort snapshot taken at request time for display in lists — never authoritative; PreviewAsync always re-reads live (ADR-010, rule 5).</summary>
    public string? SnapshotJson { get; private set; }

    public string Reason { get; private set; }

    public ApprovalRequestStatus Status { get; private set; }

    public Guid RequestedByUserId { get; private set; }

    public DateTime RequestedAt { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public DateTime? ReviewedAt { get; private set; }

    public string? ReviewNote { get; private set; }

    public DateTime? ExecutedAt { get; private set; }

    public string? ExecutionError { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public byte[] RowVersion { get; private set; }

    public static ApprovalRequest Create(
        string requestType,
        string targetEntityType,
        string targetEntityId,
        string payloadJson,
        string? snapshotJson,
        string reason,
        Guid requestedByUserId,
        DateTime requestedAtUtc,
        DateTime expiresAtUtc)
    {
        return new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            RequestType = requestType,
            TargetEntityType = targetEntityType,
            TargetEntityId = targetEntityId,
            PayloadJson = payloadJson,
            SnapshotJson = snapshotJson,
            Reason = reason,
            Status = ApprovalRequestStatus.Pending,
            RequestedByUserId = requestedByUserId,
            RequestedAt = requestedAtUtc,
            ExpiresAt = expiresAtUtc,
        };
    }

    /// <summary>Fails fast, before any permission re-check or portal call, if someone already decided this request (ADR-010, rule 3).</summary>
    public void EnsurePending()
    {
        if (Status != ApprovalRequestStatus.Pending)
        {
            throw new ConflictException("این درخواست دیگر در وضعیت «در انتظار» نیست؛ تصمیمی روی آن قبلاً ثبت شده.");
        }
    }

    public void EnsureExecutionFailed()
    {
        if (Status != ApprovalRequestStatus.ExecutionFailed)
        {
            throw new ConflictException("فقط درخواستی که اجرایش قبلاً شکست خورده قابل تلاش دوباره است.");
        }
    }

    public void MarkApproved(Guid reviewedByUserId, DateTime reviewedAtUtc, string? note)
    {
        EnsurePending();
        Status = ApprovalRequestStatus.Approved;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = reviewedAtUtc;
        ReviewNote = note;
    }

    public void MarkRejected(Guid reviewedByUserId, DateTime reviewedAtUtc, string note)
    {
        EnsurePending();
        Status = ApprovalRequestStatus.Rejected;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = reviewedAtUtc;
        ReviewNote = note;
    }

    public void MarkCancelled(DateTime cancelledAtUtc)
    {
        EnsurePending();
        Status = ApprovalRequestStatus.Cancelled;
        ReviewedAt = cancelledAtUtc;
    }

    public void MarkExecuted(DateTime executedAtUtc)
    {
        Status = ApprovalRequestStatus.Executed;
        ExecutedAt = executedAtUtc;
        ExecutionError = null;
    }

    public void MarkExecutionFailed(DateTime executedAtUtc, string error)
    {
        Status = ApprovalRequestStatus.ExecutionFailed;
        ExecutedAt = executedAtUtc;
        ExecutionError = error;
    }

    public void MarkExpired()
    {
        EnsurePending();
        Status = ApprovalRequestStatus.Expired;
    }
}
