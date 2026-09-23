namespace NafasLand.Admin.Shared.Kernel.Approvals;

/// <summary>
/// Registered per request type by the owning module (e.g. Catalog registers
/// "catalog.product.delete"), keyed in DI by <see cref="RequestType"/>
/// (<c>AddKeyedScoped&lt;IApprovalExecutor&gt;(requestType, ...)</c>) so the
/// Approvals module can resolve one without a hardcoded switch and without
/// referencing the owning module at all (ADR-010).
///
/// <see cref="RequestPermission"/> is one property beyond ADR-010's literal
/// interface sketch (which only names <see cref="RequiredPermission"/>). The ADR
/// text assigns two distinct permissions per operation — an Admin-side
/// "<c>*.request</c>" permission to file the request, and a SuperAdmin-side
/// permission, re-checked at review time, to actually decide it — but its own
/// interface sketch only has room for one. Splitting them into two properties
/// here is the resolution: <see cref="RequestPermission"/> answers "who may file
/// this?" (checked when creating the request) and <see cref="RequiredPermission"/>
/// answers "who may approve/execute this?" (checked at review time, again, since
/// the reviewer's access may have changed since the request was filed).
/// </summary>
public interface IApprovalExecutor
{
    /// <summary>e.g. "catalog.product.delete" — matches ApprovalRequest.RequestType.</summary>
    string RequestType { get; }

    /// <summary>Permission required to file a request of this type (the Admin side).</summary>
    string RequestPermission { get; }

    /// <summary>Permission required to approve/execute a request of this type (the SuperAdmin side), re-checked at execution time.</summary>
    string RequiredPermission { get; }

    /// <summary>Reads the target entity live from its source of truth — never from the request's stored SnapshotJson (ADR-010, rule 5).</summary>
    Task<ApprovalPreview> PreviewAsync(string payloadJson, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the real, irreversible action. A failure that originates from the
    /// upstream system is returned as <see cref="Result.Failure"/>, not thrown —
    /// ADR-010 rule 4 requires the request to land on ExecutionFailed with the
    /// error stored, never on an unhandled exception that could also roll back
    /// the reviewer's already-recorded decision. Automatic retry is forbidden
    /// (ADR-010, rule 4); a failed execution is retried only by an explicit
    /// reviewer action.
    /// </summary>
    Task<Result> ExecuteAsync(string payloadJson, ApprovalContext context, CancellationToken cancellationToken);
}
