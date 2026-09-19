using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NafasLand.Admin.Shared.Infrastructure.Persistence;

/// <summary>
/// Checks access to a module's database for <c>/health</c> (ADR-042). Each module
/// registers this for its own DbContext in <c>AddHealthChecks()</c>.
/// </summary>
public sealed class EfCoreDatabaseHealthCheck<TContext>(TContext dbContext) : IHealthCheck
    where TContext : DbContext
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy($"اتصال به پایگاه دادهٔ {typeof(TContext).Name} برقرار نیست.");
    }
}
