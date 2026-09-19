using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NafasLand.Admin.Shared.Infrastructure.Persistence;

/// <summary>
/// بررسی دسترسی به پایگاه دادهٔ یک ماژول برای <c>/health</c> (ADR-042). هر
/// ماژول این را برای DbContext خودش در <c>AddHealthChecks()</c> ثبت می‌کند.
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
