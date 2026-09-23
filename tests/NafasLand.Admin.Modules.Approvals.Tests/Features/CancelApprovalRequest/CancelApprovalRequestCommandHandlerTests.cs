using NafasLand.Admin.Modules.Approvals.Features.CancelApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Modules.Approvals.Tests.Fakes;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Approvals.Tests.Features.CancelApprovalRequest;

/// <summary>Only the original requester may cancel their own Pending request.</summary>
public sealed class CancelApprovalRequestCommandHandlerTests
{
    [Fact]
    public async Task درخواست‌دهنده_می‌تواند_درخواست_خودش_را_لغو_کند()
    {
        var requestedByUserId = Guid.NewGuid();
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var request = ApprovalRequest.Create(
            "test.request.type", "Product", "101", "{}", null, "دلیل", requestedByUserId, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        dbContext.ApprovalRequests.Add(request);
        await dbContext.SaveChangesAsync();

        var writer = new RecordingAuditLogWriter();
        var handler = CreateHandler(dbContext, writer, requestedByUserId);

        var result = await handler.HandleAsync(new CancelApprovalRequestCommand(request.Id), CancellationToken.None);

        Assert.Equal("Cancelled", result.Status);
        var entry = Assert.Single(writer.WrittenEntries);
        Assert.Equal(requestedByUserId, entry.ActorUserId);
        Assert.Equal(requestedByUserId, entry.OnBehalfOfUserId);
    }

    [Fact]
    public async Task کاربر_دیگر_نمی‌تواند_درخواست_شخص_دیگری_را_لغو_کند()
    {
        var requestedByUserId = Guid.NewGuid();
        var someoneElseId = Guid.NewGuid();
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var request = ApprovalRequest.Create(
            "test.request.type", "Product", "101", "{}", null, "دلیل", requestedByUserId, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        dbContext.ApprovalRequests.Add(request);
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext, new RecordingAuditLogWriter(), someoneElseId);

        await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            handler.HandleAsync(new CancelApprovalRequestCommand(request.Id), CancellationToken.None));
    }

    private static CancelApprovalRequestCommandHandler CreateHandler(ApprovalsDbContext dbContext, RecordingAuditLogWriter writer, Guid currentUserId)
    {
        var coordinator = new ApprovalExecutionCoordinator(
            new FakeApprovalExecutorRegistry(), new FakeAuthorizationService(_ => true), writer, new FakeCorrelationIdAccessor(), TimeProvider.System);
        var httpContextAccessor = new FakeHttpContextAccessor(TestHttpContext.Create(currentUserId, "Admin"));
        return new CancelApprovalRequestCommandHandler(dbContext, httpContextAccessor, coordinator);
    }
}
