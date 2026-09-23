using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Approvals.Features.CreateApprovalRequest;

/// <summary>
/// RequiredPermission is not a fixed constant — it is the filing ("*.request")
/// permission of whichever IApprovalExecutor matches RequestType, resolved by
/// CreateApprovalRequestEndpoint (which has DI access to the executor registry)
/// and passed in here as a plain field. AuthorizationBehavior only ever reads
/// this property as a getter, so a per-instance value works with the pipeline
/// exactly as-is — no change to IRequiresPermission or AuthorizationBehavior was
/// needed (this was flagged as an open question in the step prompt; this is the
/// resolution, reported in the PR description).
/// </summary>
internal sealed record CreateApprovalRequestCommand(
    string RequestType,
    string TargetEntityType,
    string TargetEntityId,
    string Reason,
    string PayloadJson,
    string RequiredPermission)
    : ICommand<CreateApprovalRequestResult>, IRequiresPermission;

internal sealed record CreateApprovalRequestResult(
    Guid Id,
    string RequestType,
    string Status,
    DateTime RequestedAt,
    DateTime? ExpiresAt);
