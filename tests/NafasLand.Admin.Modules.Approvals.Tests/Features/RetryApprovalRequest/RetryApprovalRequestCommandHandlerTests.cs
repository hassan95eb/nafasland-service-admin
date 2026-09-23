using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Features.RetryApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Modules.Approvals.Tests.Fakes;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Approvals.Tests.Features.RetryApprovalRequest;

/// <summary>ADR-010 rule 4: only an explicit retry re-runs a failed execution — never automatically.</summary>
public sealed class RetryApprovalRequestCommandHandlerTests
{
    [Fact]
    public async Task فقط_از_ExecutionFailed_قابل_تلاش_دوباره_است()
    {
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var request = ApprovalRequest.Create(
            "test.request.type", "Product", "101", "{}", null, "دلیل", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        dbContext.ApprovalRequests.Add(request);
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext, new FakeApprovalExecutor(), new RecordingAuditLogWriter(), Guid.NewGuid());

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.HandleAsync(new RetryApprovalRequestCommand(request.Id), CancellationToken.None));
    }

    [Fact]
    public async Task تلاش_دوباره_موفق_وضعیت_را_Executed_می‌کند()
    {
        var requestedByUserId = Guid.NewGuid();
        var reviewerUserId = Guid.NewGuid();
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var request = ApprovalRequest.Create(
            "test.request.type", "Product", "101", "{}", null, "دلیل", requestedByUserId, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        request.MarkApproved(Guid.NewGuid(), DateTime.UtcNow, null);
        request.MarkExecutionFailed(DateTime.UtcNow, "خطای اول");
        dbContext.ApprovalRequests.Add(request);
        await dbContext.SaveChangesAsync();

        var executor = new FakeApprovalExecutor { RequestType = "test.request.type" };
        var writer = new RecordingAuditLogWriter();
        var handler = CreateHandler(dbContext, executor, writer, reviewerUserId);

        var result = await handler.HandleAsync(new RetryApprovalRequestCommand(request.Id), CancellationToken.None);

        Assert.Equal("Executed", result.Status);
        Assert.Null(result.ExecutionError);
        Assert.Equal(1, executor.ExecuteCallCount);

        var persisted = await dbContext.ApprovalRequests.AsNoTracking().SingleAsync(entity => entity.Id == request.Id);
        Assert.Null(persisted.ExecutionError);

        var entry = Assert.Single(writer.WrittenEntries);
        Assert.Equal("ApprovalExecuted", entry.Action);
        Assert.Equal(reviewerUserId, entry.ActorUserId);
        Assert.Equal(requestedByUserId, entry.OnBehalfOfUserId);
    }

    private static RetryApprovalRequestCommandHandler CreateHandler(
        ApprovalsDbContext dbContext, FakeApprovalExecutor executor, RecordingAuditLogWriter writer, Guid reviewerUserId)
    {
        var registry = new FakeApprovalExecutorRegistry();
        registry.Register(executor);
        var coordinator = new ApprovalExecutionCoordinator(
            registry, new FakeAuthorizationService(_ => true), writer, new FakeCorrelationIdAccessor(), TimeProvider.System);
        var httpContextAccessor = new FakeHttpContextAccessor(TestHttpContext.Create(reviewerUserId, "SuperAdmin"));
        return new RetryApprovalRequestCommandHandler(dbContext, httpContextAccessor, coordinator);
    }
}
