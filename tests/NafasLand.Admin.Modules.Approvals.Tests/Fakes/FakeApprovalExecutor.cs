using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Shared.Kernel.Approvals;

namespace NafasLand.Admin.Modules.Approvals.Tests.Fakes;

internal sealed class FakeApprovalExecutor : IApprovalExecutor
{
    public string RequestType { get; init; } = "test.request.type";

    public string RequestPermission { get; init; } = "test.request";

    public string RequiredPermission { get; init; } = "test.review";

    public Func<string, CancellationToken, Task<ApprovalPreview>>? PreviewHandler { get; set; }

    public Func<string, ApprovalContext, CancellationToken, Task<Result>>? ExecuteHandler { get; set; }

    public int ExecuteCallCount { get; private set; }

    public ApprovalContext? LastContext { get; private set; }

    public Task<ApprovalPreview> PreviewAsync(string payloadJson, CancellationToken cancellationToken) =>
        PreviewHandler?.Invoke(payloadJson, cancellationToken) ?? Task.FromResult(new ApprovalPreview("پیش‌نمایش آزمایشی", []));

    public Task<Result> ExecuteAsync(string payloadJson, ApprovalContext context, CancellationToken cancellationToken)
    {
        ExecuteCallCount++;
        LastContext = context;
        return ExecuteHandler?.Invoke(payloadJson, context, cancellationToken) ?? Task.FromResult(Result.Success());
    }
}

internal sealed class FakeApprovalExecutorRegistry : IApprovalExecutorRegistry
{
    private readonly Dictionary<string, IApprovalExecutor> _executors = new(StringComparer.Ordinal);

    public void Register(IApprovalExecutor executor) => _executors[executor.RequestType] = executor;

    public IApprovalExecutor? Resolve(string requestType) => _executors.GetValueOrDefault(requestType);
}
