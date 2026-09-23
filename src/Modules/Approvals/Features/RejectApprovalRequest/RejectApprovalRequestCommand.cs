using NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Approvals.Features.RejectApprovalRequest;

internal sealed record RejectApprovalRequestCommand(Guid ApprovalRequestId, string Note)
    : ICommand<ApprovalDecisionResult>, IRequiresPermission
{
    public string RequiredPermission => ApprovalsPermissions.Review;
}
