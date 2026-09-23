using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Approvals.Persistence;

namespace NafasLand.Admin.Modules.Approvals.Tests;

/// <summary>EF Core InMemory (same sanctioned choice as Auditing's own tests) — a fresh, isolated database per test via a unique name unless a shared name is passed (for concurrency tests).</summary>
internal static class ApprovalsDbContextTestFactory
{
    public static ApprovalsDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApprovalsDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new ApprovalsDbContext(options);
    }
}
