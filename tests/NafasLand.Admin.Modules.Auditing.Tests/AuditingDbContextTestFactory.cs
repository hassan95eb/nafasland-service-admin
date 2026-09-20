using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Auditing.Persistence;

namespace NafasLand.Admin.Modules.Auditing.Tests;

/// <summary>
/// EF Core InMemory (step-02 prompt's own sanctioned choice for this module's
/// integration tests) — a fresh, isolated database per test via a unique name,
/// so tests never interfere with each other.
/// </summary>
internal static class AuditingDbContextTestFactory
{
    public static AuditingDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AuditingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AuditingDbContext(options);
    }
}
