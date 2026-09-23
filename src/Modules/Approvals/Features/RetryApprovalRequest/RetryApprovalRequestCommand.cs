using NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Approvals.Features.RetryApprovalRequest;

internal sealed record RetryApprovalRequestCommand(Guid ApprovalRequestId)
    : ICommand<ApprovalDecisionResult>, IRequiresPermission
{
    public string RequiredPermission => ApprovalsPermissions.Review;
}
