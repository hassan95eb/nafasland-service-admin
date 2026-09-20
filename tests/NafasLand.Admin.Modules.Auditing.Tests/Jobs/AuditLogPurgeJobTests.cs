using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Jobs;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Kernel.Auditing;

namespace NafasLand.Admin.Modules.Auditing.Tests.Jobs;

/// <summary>Acceptance criterion 9: archives and deletes rows older than the 6-month retention window, then logs the purge itself as AuditPurged.</summary>
public sealed class AuditLogPurgeJobTests
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

    private static AuditLog Seed(DateTime createdAt) => AuditLog.FromEntry(
        new AuditLogEntry("c", null, null, "Admin", "UserCreated", "User", "u1", null, null, null, AuditOutcome.Success, null, null, null, null),
        createdAt);

    [Fact]
    public async Task رکوردهای_قدیمی‌تر_از_۶ماه_بایگانی_و_حذف_می‌شوند_و_جدیدترها_باقی_می‌مانند()
    {
        await using var dbContext = AuditingDbContextTestFactory.Create();
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var oldRow = Seed(now.UtcDateTime.AddDays(-200));
        var recentRow = Seed(now.UtcDateTime.AddDays(-10));
        dbContext.AuditLogs.AddRange(oldRow, recentRow);
        await dbContext.SaveChangesAsync();

        var archiveDirectory = Path.Combine(Path.GetTempPath(), $"audit-archive-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(archiveDirectory);
        try
        {
            var writer = new RecordingAuditLogWriter();
            var job = new AuditLogPurgeJob(dbContext, writer, new FixedTimeProvider(now));

            await job.RunAsync(CancellationToken.None, archiveDirectory);

            var remaining = await dbContext.AuditLogs.ToListAsync();
            Assert.Single(remaining);
            Assert.Equal(recentRow.Id, remaining[0].Id);

            var archiveFile = Assert.Single(Directory.GetFiles(archiveDirectory));
            await using var fileStream = File.OpenRead(archiveFile);
            await using var gzip = new GZipStream(fileStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            var archivedContent = await reader.ReadToEndAsync();
            Assert.Contains(oldRow.Id.ToString(), archivedContent);

            var purgeEntry = Assert.Single(writer.WrittenEntries);
            Assert.Equal("AuditPurged", purgeEntry.Action);
            Assert.Equal("System", purgeEntry.ActorRoleAtTime);
            Assert.Null(purgeEntry.ActorUserId);
        }
        finally
        {
            Directory.Delete(archiveDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task وقتی_هیچ_رکورد_قدیمی‌ای_نیست_کاری_نمی‌کند_و_چیزی_ثبت_نمی‌کند()
    {
        await using var dbContext = AuditingDbContextTestFactory.Create();
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        dbContext.AuditLogs.Add(Seed(now.UtcDateTime.AddDays(-10)));
        await dbContext.SaveChangesAsync();

        var archiveDirectory = Path.Combine(Path.GetTempPath(), $"audit-archive-test-{Guid.NewGuid()}");
        var writer = new RecordingAuditLogWriter();
        var job = new AuditLogPurgeJob(dbContext, writer, new FixedTimeProvider(now));

        await job.RunAsync(CancellationToken.None, archiveDirectory);

        Assert.Empty(writer.WrittenEntries);
        Assert.False(Directory.Exists(archiveDirectory));
        Assert.Single(await dbContext.AuditLogs.ToListAsync());
    }
}
