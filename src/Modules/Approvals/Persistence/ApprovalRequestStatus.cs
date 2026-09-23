namespace NafasLand.Admin.Modules.Approvals.Persistence;

/// <summary>ADR-010's status machine. Pending is the only non-terminal state.</summary>
internal enum ApprovalRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Executed = 3,
    ExecutionFailed = 4,
    Cancelled = 5,
    Expired = 6,
}
