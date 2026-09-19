using Hangfire;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NafasLand.Admin.Api.HealthChecks;

/// <summary>
/// Checks the job runner's status for <c>/health</c> (ADR-042). At this step no
/// real job exists; it only checks that the storage is reachable.
/// </summary>
internal sealed class HangfireHealthCheck(JobStorage jobStorage) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            jobStorage.GetMonitoringApi().Servers();
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (Exception exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("ذخیره‌سازی Hangfire در دسترس نیست.", exception));
        }
    }
}
