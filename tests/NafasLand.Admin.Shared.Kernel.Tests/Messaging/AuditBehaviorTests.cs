using Microsoft.AspNetCore.Http;
using NafasLand.Admin.Shared.Infrastructure.Auditing;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Kernel.Tests.Messaging;

public sealed class AuditBehaviorTests
{
    private sealed record FakeCommand : ICommand<string>;

    private sealed record AuditableCommand : ICommand<string>, IAuditableCommand
    {
        public string AuditAction => "SomethingHappened";
        public string AuditEntityType => "Something";
    }

    private sealed class FakeCorrelationIdAccessor : ICorrelationIdAccessor
    {
        public string CorrelationId => "test-correlation-id";
    }

    private sealed class FakeAuditLogWriter : IAuditLogWriter
    {
        public List<AuditLogEntry> WrittenEntries { get; } = [];

        public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
        {
            WrittenEntries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private static AuditBehavior<TCommand, string> BuildBehavior<TCommand>(
        AuditContext auditContext,
        FakeAuditLogWriter auditLogWriter)
        where TCommand : ICommand<string>
    {
        return new AuditBehavior<TCommand, string>(
            auditContext,
            auditLogWriter,
            new HttpContextAccessor(),
            new FakeCorrelationIdAccessor());
    }

    [Fact]
    public async Task این_گام_برای_command_غیر_IAuditableCommand_فقط_عبور_می‌دهد_و_نتیجهٔ_next_را_برمی‌گرداند()
    {
        var auditLogWriter = new FakeAuditLogWriter();
        var behavior = BuildBehavior<FakeCommand>(new AuditContext(), auditLogWriter);

        var result = await behavior.HandleAsync(new FakeCommand(), () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Empty(auditLogWriter.WrittenEntries);
    }

    [Fact]
    public async Task اجرای_موفق_یک_IAuditableCommand_رکورد_Success_می‌نویسد()
    {
        var auditContext = new AuditContext();
        auditContext.SetAfter(new { Name = "ali" });
        var auditLogWriter = new FakeAuditLogWriter();
        var behavior = BuildBehavior<AuditableCommand>(auditContext, auditLogWriter);

        var result = await behavior.HandleAsync(new AuditableCommand(), () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
        var entry = Assert.Single(auditLogWriter.WrittenEntries);
        Assert.Equal(AuditOutcome.Success, entry.Outcome);
        Assert.Equal("SomethingHappened", entry.Action);
        Assert.Equal("Something", entry.EntityType);
        Assert.Null(entry.FailureReason);
    }

    [Fact]
    public async Task خطای_handler_روی_یک_IAuditableCommand_رکورد_Failed_می‌نویسد_و_استثنا_را_دوباره_پرتاب_می‌کند()
    {
        var auditLogWriter = new FakeAuditLogWriter();
        var behavior = BuildBehavior<AuditableCommand>(new AuditContext(), auditLogWriter);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.HandleAsync(new AuditableCommand(), () => throw new InvalidOperationException("خطای دیتابیس"), CancellationToken.None));

        var entry = Assert.Single(auditLogWriter.WrittenEntries);
        Assert.Equal(AuditOutcome.Failed, entry.Outcome);
        Assert.Equal("خطای دیتابیس", entry.FailureReason);
    }
}
