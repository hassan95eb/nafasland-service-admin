using NafasLand.Admin.Modules.Approvals;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;

internal sealed record ApproveApprovalRequestCommand(Guid ApprovalRequestId, string? Note)
    : ICommand<ApprovalDecisionResult>, IRequiresPermission
{
    public string RequiredPermission => ApprovalsPermissions.Review;
}

/// <summary>Shared response shape for every decision endpoint (approve/reject/cancel/retry).</summary>
internal sealed record ApprovalDecisionResult(Guid Id, string Status, string? ExecutionError);
