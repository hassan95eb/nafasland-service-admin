using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Auditing.Configuration;
using NafasLand.Admin.Modules.Auditing.Features.ExportAuditLog;
using NafasLand.Admin.Modules.Auditing.Features.Queries;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Auditing;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Auditing.Tests.Features.ExportAuditLog;

public sealed class ExportAuditLogCommandHandlerTests
{
    private static readonly AuditLogFilter EmptyFilter = new(null, null, null, null, null, null);

    private static ExportAuditLogCommandHandler BuildHandler(
        AuditingDbContext dbContext,
        IAuditContext auditContext,
        FakeBackgroundJobClient backgroundJobClient,
        int synchronousRowThreshold)
    {
        var options = Options.Create(new AuditExportOptions { SynchronousRowThreshold = synchronousRowThreshold });
        return new ExportAuditLogCommandHandler(dbContext, auditContext, backgroundJobClient, options, new FakeUserDirectory(), TimeProvider.System);
    }

    [Fact]
    public async Task با_تعداد_ردیف_کمتر_از_آستانه_فایل_را_همان‌جا_synchronous_تولید_می‌کند()
    {
        await using var dbContext = AuditingDbContextTestFactory.Create();
        dbContext.AuditLogs.Add(AuditLog.FromEntry(
            new AuditLogEntry("c1", null, null, "Admin", "UserCreated", "User", "u1", null, null, null, AuditOutcome.Success, null, null, null, null),
            DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        var backgroundJobClient = new FakeBackgroundJobClient();
        var handler = BuildHandler(dbContext, new AuditContext(), backgroundJobClient, synchronousRowThreshold: 25_000);

        var result = await handler.HandleAsync(new ExportAuditLogCommand(EmptyFilter, "csv"), CancellationToken.None);

        Assert.False(result.IsAsync);
        Assert.NotNull(result.FileBytes);
        Assert.Equal("text/csv", result.ContentType);
        Assert.Empty(backgroundJobClient.CreatedJobs);
    }

    [Fact]
    public async Task با_تعداد_ردیف_بیشتر_از_آستانهٔ_تزریق‌شده_یک_job_صف_می‌کند_و_jobId_برمی‌گرداند()
    {
        await using var dbContext = AuditingDbContextTestFactory.Create();
        for (var i = 0; i < 3; i++)
        {
            dbContext.AuditLogs.Add(AuditLog.FromEntry(
                new AuditLogEntry($"c{i}", null, null, "Admin", "UserCreated", "User", "u1", null, null, null, AuditOutcome.Success, null, null, null, null),
                DateTime.UtcNow));
        }

        await dbContext.SaveChangesAsync();

        var backgroundJobClient = new FakeBackgroundJobClient();
        // Injected threshold of 1 simulates the >25,000-row path without generating that many rows.
        var handler = BuildHandler(dbContext, new AuditContext(), backgroundJobClient, synchronousRowThreshold: 1);

        var result = await handler.HandleAsync(new ExportAuditLogCommand(EmptyFilter, "csv"), CancellationToken.None);

        Assert.True(result.IsAsync);
        Assert.NotNull(result.JobId);
        Assert.Null(result.FileBytes);
        Assert.Single(backgroundJobClient.CreatedJobs);

        var queuedJob = await dbContext.AuditExportJobs.FindAsync(result.JobId);
        Assert.NotNull(queuedJob);
        Assert.Equal(AuditExportJobStatus.Queued, queuedJob!.Status);
    }

    [Fact]
    public async Task اجرای_ExportAuditLogCommand_از_طریق_AuditBehavior_خودش_یک_رکورد_AuditExported_تولید_می‌کند()
    {
        await using var dbContext = AuditingDbContextTestFactory.Create();
        var auditContext = new AuditContext();
        var auditLogWriter = new AuditLogWriter(dbContext, TimeProvider.System);
        var backgroundJobClient = new FakeBackgroundJobClient();
        var handler = BuildHandler(dbContext, auditContext, backgroundJobClient, synchronousRowThreshold: 25_000);

        var behavior = new AuditBehavior<ExportAuditLogCommand, ExportAuditLogResult>(
            auditContext,
            auditLogWriter,
            new HttpContextAccessor(),
            new FakeCorrelationIdAccessor());

        var command = new ExportAuditLogCommand(EmptyFilter, "csv");
        await behavior.HandleAsync(command, () => handler.HandleAsync(command, CancellationToken.None), CancellationToken.None);

        var writtenLog = Assert.Single(dbContext.AuditLogs);
        Assert.Equal("AuditExported", writtenLog.Action);
        Assert.Equal(AuditOutcome.Success, writtenLog.Outcome);
        Assert.NotNull(writtenLog.AfterJson);
    }

    private sealed class FakeCorrelationIdAccessor : ICorrelationIdAccessor
    {
        public string CorrelationId => "test-correlation-id";
    }
}
