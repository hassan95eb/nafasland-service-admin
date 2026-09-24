using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Features.CreateApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Modules.Approvals.Tests.Fakes;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Approvals.Tests.Features.CreateApprovalRequest;

public sealed class CreateApprovalRequestCommandHandlerTests
{
    [Fact]
    public async Task درخواست_تازه_را_Pending_با_انقضای_۷روزه_ثبت_می‌کند()
    {
        var requestedByUserId = Guid.NewGuid();
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var executor = new FakeApprovalExecutor { RequestType = "test.request.type" };
        var writer = new RecordingAuditLogWriter();
        var auditContext = new CapturingAuditContext();
        var handler = CreateHandler(dbContext, executor, writer, requestedByUserId, auditContext);

        var command = new CreateApprovalRequestCommand("test.request.type", "Product", "101", "دلیل درخواست", "{\"productId\":\"101\"}", "test.request");
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal("Pending", result.Status);
        var persisted = await dbContext.ApprovalRequests.AsNoTracking().SingleAsync(request => request.Id == result.Id);
        Assert.Equal(requestedByUserId, persisted.RequestedByUserId);
        Assert.Equal(persisted.RequestedAt.AddDays(7), persisted.ExpiresAt);

        // The row itself is AuditBehavior's job now (the command is IAuditableCommand);
        // the handler only fills the context it reads from.
        Assert.Empty(writer.WrittenEntries);
        Assert.Equal("101", auditContext.EntityId);
        Assert.Contains(result.Id.ToString(), System.Text.Json.JsonSerializer.Serialize(auditContext.After));
    }

    [Fact]
    public void ثبت_درخواست_از_مسیر_استاندارد_audit_عبور_می‌کند_تا_Denied_و_Failed_هم_ثبت_شوند()
    {
        var command = new CreateApprovalRequestCommand("test.request.type", "Variant", "501", "دلیل", "{}", "test.request");

        var auditable = Assert.IsAssignableFrom<IAuditableCommand>(command);
        Assert.Equal("ApprovalRequested", auditable.AuditAction);
        Assert.Equal("Variant", auditable.AuditEntityType);
    }

    [Fact]
    public async Task محافظ_محصول_تستی_در_لحظهٔ_ثبت_درخواست_هم_رد_می‌کند()
    {
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var executor = new FakeApprovalExecutor
        {
            RequestType = "test.request.type",
            PreviewHandler = (_, _) => throw new AuthorizationDeniedException("محصول تستی نیست."),
        };
        var auditContext = new CapturingAuditContext();
        var handler = CreateHandler(dbContext, executor, new RecordingAuditLogWriter(), Guid.NewGuid(), auditContext);

        var command = new CreateApprovalRequestCommand("test.request.type", "Product", "999", "دلیل", "{\"productId\":\"999\"}", "test.request");

        await Assert.ThrowsAsync<AuthorizationDeniedException>(() => handler.HandleAsync(command, CancellationToken.None));
        Assert.Empty(await dbContext.ApprovalRequests.ToListAsync());

        // AuditBehavior's Failed row for this attempt still names its target.
        Assert.Equal("999", auditContext.EntityId);
    }

    [Fact]
    public async Task نبود_پیش‌نمایش_زنده_مانع_ثبت_درخواست_نمی‌شود()
    {
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var executor = new FakeApprovalExecutor
        {
            RequestType = "test.request.type",
            PreviewHandler = (_, _) => throw new ResourceNotFoundException("محصول پیدا نشد."),
        };
        var handler = CreateHandler(dbContext, executor, new RecordingAuditLogWriter(), Guid.NewGuid());

        var command = new CreateApprovalRequestCommand("test.request.type", "Product", "101", "دلیل", "{\"productId\":\"101\"}", "test.request");
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal("Pending", result.Status);
    }

    private static CreateApprovalRequestCommandHandler CreateHandler(
        ApprovalsDbContext dbContext,
        FakeApprovalExecutor executor,
        RecordingAuditLogWriter writer,
        Guid requestedByUserId,
        CapturingAuditContext? auditContext = null)
    {
        var registry = new FakeApprovalExecutorRegistry();
        registry.Register(executor);
        var coordinator = new ApprovalExecutionCoordinator(
            registry, new FakeAuthorizationService(_ => true), writer, new FakeCorrelationIdAccessor(), TimeProvider.System);
        var httpContextAccessor = new FakeHttpContextAccessor(TestHttpContext.Create(requestedByUserId, "Admin"));
        return new CreateApprovalRequestCommandHandler(dbContext, registry, httpContextAccessor, coordinator, auditContext ?? new CapturingAuditContext());
    }

    private sealed class CapturingAuditContext : IAuditContext
    {
        public string? EntityId { get; private set; }

        public object? After { get; private set; }

        public void SetEntityId(string entityId) => EntityId = entityId;

        public void SetParentEntity(string entityType, string entityId)
        {
        }

        public void SetBefore(object? snapshot)
        {
        }

        public void SetAfter(object? snapshot) => After = snapshot;
    }
}
