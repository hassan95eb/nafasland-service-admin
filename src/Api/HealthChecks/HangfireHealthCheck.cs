using Hangfire;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NafasLand.Admin.Api.HealthChecks;

/// <summary>
/// بررسی وضعیت job runner برای <c>/health</c> (ADR-042). در این گام هیچ job
/// واقعی‌ای وجود ندارد؛ فقط بررسی می‌شود storage در دسترس است.
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
