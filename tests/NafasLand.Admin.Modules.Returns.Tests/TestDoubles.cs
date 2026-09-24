using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Returns.Infrastructure;
using NafasLand.Admin.Modules.Returns.Persistence;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Users;

namespace NafasLand.Admin.Modules.Returns.Tests;

/// <summary>The fixture order (synthetic personal data, same shape as a real response) and variations of it.</summary>
internal static class OrderFixtures
{
    public const long OrderId = 900000001;

    /// <summary>1790231819 = 2026-09-24 06:36:59 UTC (10:06 Tehran).</summary>
    public static readonly DateTime CreatedAtUtc = new(2026, 9, 24, 6, 36, 59, DateTimeKind.Utc);

    /// <summary>"Now" for every test: the day after the order, Tehran time.</summary>
    public static readonly DateTimeOffset Now = new(2026, 9, 25, 8, 0, 0, TimeSpan.Zero);

    public static string ReadJson(string name = "order-paid.json") =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    public static PortalOrder Paid(long orderId = OrderId) => PortalOrderMapper.Map(ReadJson())! with { OrderId = orderId };

    public static PortalOrder WithStatuses(params string[] statuses) => Paid() with { Statuses = statuses };
}

internal sealed class FakePortalOrderClient : IPortalOrderClient
{
    private readonly Dictionary<long, PortalOrder> _orders = [];

    public Exception? Failure { get; set; }

    public int CallCount { get; private set; }

    public FakePortalOrderClient With(PortalOrder order)
    {
        _orders[order.OrderId] = order;
        return this;
    }

    public Task<PortalOrder?> GetOrderAsync(long orderId, CancellationToken cancellationToken)
    {
        CallCount++;
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult(_orders.GetValueOrDefault(orderId));
    }

    public static FakePortalOrderClient Unavailable() => new() { Failure = new PortalUnavailableException() };
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class FakeUserDirectory(IReadOnlyDictionary<Guid, string>? usernames = null) : IUserDirectory
{
    private readonly IReadOnlyDictionary<Guid, string> _usernames = usernames ?? new Dictionary<Guid, string>();

    public Task<IReadOnlyDictionary<Guid, string>> GetUsernamesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(
            _usernames.Where(pair => userIds.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value));
}

internal sealed class RecordingAuditLogWriter : IAuditLogWriter
{
    private readonly List<AuditLogEntry> _entries = [];

    public IReadOnlyList<AuditLogEntry> Entries
    {
        get
        {
            lock (_entries)
            {
                return _entries.ToList();
            }
        }
    }

    public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        lock (_entries)
        {
            _entries.Add(entry);
        }

        return Task.CompletedTask;
    }
}

internal static class ReturnsDbContextTestFactory
{
    public static ReturnsDbContext Create(string? databaseName = null) =>
        new(new DbContextOptionsBuilder<ReturnsDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options);
}
