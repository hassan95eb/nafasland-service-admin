using NafasLand.Admin.Shared.Kernel.Approvals;

namespace NafasLand.Admin.Modules.Approvals.Infrastructure;

/// <summary>
/// Resolves an <see cref="IApprovalExecutor"/> by its RequestType through keyed
/// DI — no switch statement, no reference to any owning module (ADR-010). A
/// module adds a new request type entirely on its own side by registering
/// itself with <c>AddKeyedScoped&lt;IApprovalExecutor&gt;(requestType, ...)</c>.
/// </summary>
internal interface IApprovalExecutorRegistry
{
    IApprovalExecutor? Resolve(string requestType);
}
