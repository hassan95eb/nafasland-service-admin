using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Modules.Approvals.Tests.Fakes;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Approvals.Tests.Features.ApproveApprovalRequest;

/// <summary>
/// Covers ADR-010 rules 2 (reviewer permission re-checked at execution time), 3
/// (terminal decision rejected) and 4 (execution failure never rolls back the
/// approval decision; no automatic retry) plus dual-actor auditing (ADR-030).
/// </summary>
public sealed class ApproveApprovalRequestCommandHandlerTests
{
    private const string RequestType = "test.request.type";

    [Fact]
    public async Task تأیید_موفق_را_اجرا_و_Executed_می‌کند_و_دو_رویداد_ثبت_می‌کند()
    {
        var requestedByUserId = Guid.NewGuid();
        var reviewerUserId = Guid.NewGuid();
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var request = SeedPendingRequest(dbContext, requestedByUserId);

        var executor = new FakeApprovalExecutor { RequestType = RequestType };
        var writer = new RecordingAuditLogWriter();
        var handler = CreateHandler(dbContext, executor, writer, reviewerUserId, isAuthorized: true);

        var result = await handler.HandleAsync(new ApproveApprovalRequestCommand(request.Id, "تأیید شد"), CancellationToken.None);

        Assert.Equal("Executed", result.Status);
        Assert.Equal(1, executor.ExecuteCallCount);
        Assert.Equal(reviewerUserId, executor.LastContext!.ReviewedByUserId);
        Assert.Equal(requestedByUserId, executor.LastContext.RequestedByUserId);

        Assert.Equal(2, writer.WrittenEntries.Count);
        var approvedEntry = writer.WrittenEntries.Single(entry => entry.Action == "ApprovalApproved");
        var executedEntry = writer.WrittenEntries.Single(entry => entry.Action == "ApprovalExecuted");
        foreach (var entry in new[] { approvedEntry, executedEntry })
        {
            Assert.Equal(reviewerUserId, entry.ActorUserId);
            Assert.Equal(requestedByUserId, entry.OnBehalfOfUserId);
        }
    }

    [Fact]
    public async Task شکست_اجرا_وضعیت_را_ExecutionFailed_می‌کند_و_تصمیم_تأیید_از_دست_نمی‌رود()
    {
        var reviewerUserId = Guid.NewGuid();
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var request = SeedPendingRequest(dbContext, Guid.NewGuid());

        var executor = new FakeApprovalExecutor
        {
            RequestType = RequestType,
            ExecuteHandler = (_, _, _) => Task.FromResult(Result.Failure("پرتال در دسترس نیست.")),
        };
        var writer = new RecordingAuditLogWriter();
        var handler = CreateHandler(dbContext, executor, writer, reviewerUserId, isAuthorized: true);

        var result = await handler.HandleAsync(new ApproveApprovalRequestCommand(request.Id, null), CancellationToken.None);

        Assert.Equal("ExecutionFailed", result.Status);
        Assert.Equal("پرتال در دسترس نیست.", result.ExecutionError);

        var persisted = await dbContext.ApprovalRequests.AsNoTracking().SingleAsync(entity => entity.Id == request.Id);
        Assert.Equal(ApprovalRequestStatus.ExecutionFailed, persisted.Status);
        Assert.NotNull(persisted.ReviewedByUserId);

        Assert.Contains(writer.WrittenEntries, entry => entry.Action == "ApprovalApproved");
        var failedEntry = writer.WrittenEntries.Single(entry => entry.Action == "ApprovalExecutionFailed");
        Assert.Equal("پرتال در دسترس نیست.", failedEntry.FailureReason);
    }

    [Fact]
    public async Task اجرا_استثنا_بدهد_هم_ExecutionFailed_می‌شود_نه_شکست_کل_درخواست()
    {
        var reviewerUserId = Guid.NewGuid();
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var request = SeedPendingRequest(dbContext, Guid.NewGuid());

        var executor = new FakeApprovalExecutor
        {
            RequestType = RequestType,
            ExecuteHandler = (_, _, _) => throw new InvalidOperationException("خطای غیرمنتظره"),
        };
        var handler = CreateHandler(dbContext, executor, new RecordingAuditLogWriter(), reviewerUserId, isAuthorized: true);

        var result = await handler.HandleAsync(new ApproveApprovalRequestCommand(request.Id, null), CancellationToken.None);

        Assert.Equal("ExecutionFailed", result.Status);
    }

    [Fact]
    public async Task دسترسی_تغییریافتهٔ_تأییدکننده_اجرا_نمی‌شود_و_درخواست_Pending_می‌ماند()
    {
        var reviewerUserId = Guid.NewGuid();
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var request = SeedPendingRequest(dbContext, Guid.NewGuid());

        var executor = new FakeApprovalExecutor { RequestType = RequestType };
        var handler = CreateHandler(dbContext, executor, new RecordingAuditLogWriter(), reviewerUserId, isAuthorized: false);

        await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            handler.HandleAsync(new ApproveApprovalRequestCommand(request.Id, null), CancellationToken.None));

        Assert.Equal(0, executor.ExecuteCallCount);
        var persisted = await dbContext.ApprovalRequests.AsNoTracking().SingleAsync(entity => entity.Id == request.Id);
        Assert.Equal(ApprovalRequestStatus.Pending, persisted.Status);
    }

    [Fact]
    public async Task تصمیم_روی_درخواست_ترمینال_409_می‌دهد()
    {
        var reviewerUserId = Guid.NewGuid();
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var request = SeedPendingRequest(dbContext, Guid.NewGuid());
        request.MarkRejected(Guid.NewGuid(), DateTime.UtcNow, "قبلاً رد شد");
        await dbContext.SaveChangesAsync();

        var executor = new FakeApprovalExecutor { RequestType = RequestType };
        var handler = CreateHandler(dbContext, executor, new RecordingAuditLogWriter(), reviewerUserId, isAuthorized: true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.HandleAsync(new ApproveApprovalRequestCommand(request.Id, null), CancellationToken.None));
    }

    private static ApprovalRequest SeedPendingRequest(ApprovalsDbContext dbContext, Guid requestedByUserId)
    {
        var request = ApprovalRequest.Create(RequestType, "Product", "101", "{}", null, "دلیل", requestedByUserId, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        dbContext.ApprovalRequests.Add(request);
        dbContext.SaveChanges();
        return request;
    }

    private static ApproveApprovalRequestCommandHandler CreateHandler(
        ApprovalsDbContext dbContext,
        FakeApprovalExecutor executor,
        RecordingAuditLogWriter writer,
        Guid reviewerUserId,
        bool isAuthorized)
    {
        var registry = new FakeApprovalExecutorRegistry();
        registry.Register(executor);

        var coordinator = new ApprovalExecutionCoordinator(
            registry,
            new FakeAuthorizationService(_ => isAuthorized),
            writer,
            new FakeCorrelationIdAccessor(),
            TimeProvider.System);

        var httpContextAccessor = new FakeHttpContextAccessor(TestHttpContext.Create(reviewerUserId, "SuperAdmin"));
        return new ApproveApprovalRequestCommandHandler(dbContext, httpContextAccessor, coordinator);
    }
}
