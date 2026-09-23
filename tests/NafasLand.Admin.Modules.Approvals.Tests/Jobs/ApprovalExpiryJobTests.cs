using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Jobs;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Approvals.Tests.Jobs;

/// <summary>Acceptance criterion 8: a Pending request older than 7 days expires automatically.</summary>
public sealed class ApprovalExpiryJobTests
{
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingAuditLogWriter : IAuditLogWriter
    {
        public List<AuditLogEntry> WrittenEntries { get; } = [];

        public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
        {
            WrittenEntries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private static ApprovalRequest CreateRequest(DateTime requestedAtUtc, Guid requestedByUserId) => ApprovalRequest.Create(
        "catalog.product.delete", "Product", "101", "{}", null, "دلیل", requestedByUserId, requestedAtUtc, requestedAtUtc.AddDays(7));

    [Fact]
    public async Task درخواست‌های_Pending_قدیمی‌تر_از_۷روز_منقضی_می‌شوند_و_تازه‌ها_دست‌نخورده_می‌مانند()
    {
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var requestedByUserId = Guid.NewGuid();

        var oldRequest = CreateRequest(now.UtcDateTime.AddDays(-8), requestedByUserId);
        var recentRequest = CreateRequest(now.UtcDateTime.AddDays(-1), requestedByUserId);
        dbContext.ApprovalRequests.AddRange(oldRequest, recentRequest);
        await dbContext.SaveChangesAsync();

        var writer = new RecordingAuditLogWriter();
        var job = new ApprovalExpiryJob(dbContext, writer, new FixedTimeProvider(now));

        await job.RunAsync(CancellationToken.None);

        var refreshedOld = await dbContext.ApprovalRequests.AsNoTracking().SingleAsync(request => request.Id == oldRequest.Id);
        var refreshedRecent = await dbContext.ApprovalRequests.AsNoTracking().SingleAsync(request => request.Id == recentRequest.Id);

        Assert.Equal(ApprovalRequestStatus.Expired, refreshedOld.Status);
        Assert.Equal(ApprovalRequestStatus.Pending, refreshedRecent.Status);

        var entry = Assert.Single(writer.WrittenEntries);
        Assert.Equal("ApprovalExpired", entry.Action);
        Assert.Equal("System", entry.ActorRoleAtTime);
        Assert.Null(entry.ActorUserId);
        Assert.Equal(requestedByUserId, entry.OnBehalfOfUserId);
    }

    [Fact]
    public async Task وقتی_هیچ_درخواست_قدیمی‌ای_نیست_کاری_نمی‌کند()
    {
        await using var dbContext = ApprovalsDbContextTestFactory.Create();
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        dbContext.ApprovalRequests.Add(CreateRequest(now.UtcDateTime.AddDays(-1), Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var writer = new RecordingAuditLogWriter();
        var job = new ApprovalExpiryJob(dbContext, writer, new FixedTimeProvider(now));

        await job.RunAsync(CancellationToken.None);

        Assert.Empty(writer.WrittenEntries);
        Assert.Single(await dbContext.ApprovalRequests.ToListAsync());
    }
}
