using NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Approvals.Features.CancelApprovalRequest;

/// <summary>
/// No fixed permission: any authenticated user may attempt this, but only the
/// original requester may actually cancel their own request — the handler
/// checks RequestedByUserId itself (same shape as "approval.read.own" in
/// ADR-010, which is likewise not a grantable permission under the
/// default-closed model).
/// </summary>
internal sealed record CancelApprovalRequestCommand(Guid ApprovalRequestId)
    : ICommand<ApprovalDecisionResult>, IRequiresAuthenticatedUser;
