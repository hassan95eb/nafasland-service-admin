using NafasLand.Admin.Shared.Kernel.Auditing;
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
///
/// Unlike the decision commands, this one is an IAuditableCommand: filing is a
/// single-actor event (requester == actor, nothing "on behalf of"), so the
/// standard AuditBehavior/AuthorizationBehavior path fits — and it is the only
/// way a filing that is refused (missing "*.request" permission → Denied, ADR-002)
/// or that fails before the request row exists (e.g. the dev test-product guard
/// in PreviewAsync → Failed) still reaches AuditLog. A hand-written audit call at
/// the end of the handler, as before, only ever saw the success path.
/// </summary>
internal sealed record CreateApprovalRequestCommand(
    string RequestType,
    string TargetEntityType,
    string TargetEntityId,
    string Reason,
    string PayloadJson,
    string RequiredPermission)
    : ICommand<CreateApprovalRequestResult>, IRequiresPermission, IAuditableCommand
{
    public string AuditAction => "ApprovalRequested";

    public string AuditEntityType => TargetEntityType;
}

internal sealed record CreateApprovalRequestResult(
    Guid Id,
    string RequestType,
    string Status,
    DateTime RequestedAt,
    DateTime? ExpiresAt);
