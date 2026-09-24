namespace NafasLand.Admin.Shared.Kernel.Approvals;

/// <summary>
/// Passed to <see cref="IApprovalExecutor.ExecuteAsync"/> so an executor can
/// report the approval event without knowing anything about the Approvals
/// module's own entity or persistence (ADR-010: the Approvals module is generic,
/// executors live in the owning module). The two timestamps are the request's
/// own filing and decision times, not the execution time — after an
/// ExecutionFailed and a manual retry, "now" is the retry, not the approval.
/// </summary>
public sealed record ApprovalContext(
    Guid ApprovalRequestId,
    Guid ReviewedByUserId,
    Guid RequestedByUserId,
    string CorrelationId,
    DateTime RequestedAtUtc,
    DateTime ReviewedAtUtc);
