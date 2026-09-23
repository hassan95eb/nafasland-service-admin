using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Kernel.Approvals;

namespace NafasLand.Admin.Modules.Approvals.Infrastructure;

internal sealed class ApprovalExecutorRegistry(IServiceProvider serviceProvider) : IApprovalExecutorRegistry
{
    public IApprovalExecutor? Resolve(string requestType) =>
        serviceProvider.GetKeyedService<IApprovalExecutor>(requestType);
}
