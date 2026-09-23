using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Features.RejectApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Modules.Approvals.Tests.Fakes;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Approvals.Tests.Features.RejectApprovalRequest;

/// <summary>Acceptance criterion 3: rejecting never calls the portal.</summary>
public sealed class RejectApprovalRequestCommandHandlerTests
{
    [Fact]
    public async Task رد_موفق_هیچ_تماسی_با_مجری_نمی‌زند_و_رویداد_با_دو_عامل_ثبت_می‌کند()
    {
        var requestedByUserId = Guid.NewGuid();
        var reviewerUserId = Guid.NewGuid();
        await using var dbContext = ApprovalsDbContextTestFactory.Create();

        var request = ApprovalRequest.Create(
            "test.request.type", "Product", "101", "{}", null, "دلیل", requestedByUserId, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        dbContext.ApprovalRequests.Add(request);
        await dbContext.SaveChangesAsync();

        var executor = new FakeApprovalExecutor { RequestType = "test.request.type" };
        var registry = new FakeApprovalExecutorRegistry();
        registry.Register(executor);
        var writer = new RecordingAuditLogWriter();
        var coordinator = new ApprovalExecutionCoordinator(
            registry, new FakeAuthorizationService(_ => true), writer, new FakeCorrelationIdAccessor(), TimeProvider.System);
        var httpContextAccessor = new FakeHttpContextAccessor(TestHttpContext.Create(reviewerUserId, "SuperAdmin"));
        var handler = new RejectApprovalRequestCommandHandler(dbContext, httpContextAccessor, coordinator);

        var result = await handler.HandleAsync(new RejectApprovalRequestCommand(request.Id, "دلیل رد"), CancellationToken.None);

        Assert.Equal("Rejected", result.Status);
        Assert.Equal(0, executor.ExecuteCallCount);

        var entry = Assert.Single(writer.WrittenEntries);
        Assert.Equal("ApprovalRejected", entry.Action);
        Assert.Equal(reviewerUserId, entry.ActorUserId);
        Assert.Equal(requestedByUserId, entry.OnBehalfOfUserId);
    }

    [Fact]
    public async Task تصمیم_روی_درخواست_ترمینال_409_می‌دهد()
    {
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var request = ApprovalRequest.Create(
            "test.request.type", "Product", "101", "{}", null, "دلیل", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        request.MarkCancelled(DateTime.UtcNow);
        dbContext.ApprovalRequests.Add(request);
        await dbContext.SaveChangesAsync();

        var registry = new FakeApprovalExecutorRegistry();
        var coordinator = new ApprovalExecutionCoordinator(
            registry, new FakeAuthorizationService(_ => true), new RecordingAuditLogWriter(), new FakeCorrelationIdAccessor(), TimeProvider.System);
        var httpContextAccessor = new FakeHttpContextAccessor(TestHttpContext.Create(Guid.NewGuid(), "SuperAdmin"));
        var handler = new RejectApprovalRequestCommandHandler(dbContext, httpContextAccessor, coordinator);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.HandleAsync(new RejectApprovalRequestCommand(request.Id, "دوباره"), CancellationToken.None));
    }
}
