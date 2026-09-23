namespace NafasLand.Admin.Modules.Approvals;

/// <summary>
/// Permission keys for the Approvals module itself (ADR-005). Both are
/// IsSuperAdminOnly (ADR-002): only a SuperAdmin reviews a request or sees the
/// full cartable. "Read your own requests" is deliberately not a permission —
/// ADR-010 lists it as available to every role, which under the default-closed
/// model means it cannot be a grantable permission at all; see
/// Features/Queries/ListApprovalRequestsEndpoint for how that is enforced instead.
/// </summary>
internal static class ApprovalsPermissions
{
    public const string Review = "approvals.review";
    public const string ReadAll = "approvals.read.all";
}
