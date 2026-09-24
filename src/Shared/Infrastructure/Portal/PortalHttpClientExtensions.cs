using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace NafasLand.Admin.Shared.Infrastructure.Portal;

public static class PortalHttpClientExtensions
{
    /// <summary>
    /// The only way a module gets an HttpClient to the portal (ADR-054): base
    /// address, token, resilience (ADR-050) and the one shared rate limiter
    /// (ADR-026) are wired here together, so no module can build a client that
    /// skips the limiter. Calling it from several modules registers the shared
    /// pieces once; every typed client resolves the same PortalRateLimiter.
    /// </summary>
    public static IHttpClientBuilder AddPortalHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TClient : class
        where TImplementation : class, TClient
    {
        var section = configuration.GetSection(PortalConnectionOptions.SectionName);
        var portalOptions = section.Get<PortalConnectionOptions>() ?? new PortalConnectionOptions();

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(PortalRateLimiter)))
        {
            services.AddOptions<PortalConnectionOptions>().Bind(section);
            services.AddSingleton<PortalRateLimiter>();
        }

        services.TryAddSingleton<IPortalTokenProvider, PortalTokenProvider>();
        services.TryAddTransient<PortalRateLimitingHandler>();
        services.TryAddTransient<PortalRequestHeadersHandler>();

        var httpClient = services.AddHttpClient<TClient, TImplementation>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PortalConnectionOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + '/', UriKind.Absolute);
        });
        httpClient.RedactLoggedHeaders(headerName =>
            string.Equals(headerName, "Authorization", StringComparison.OrdinalIgnoreCase));

        httpClient.AddResilienceHandler("portal-read", pipeline =>
        {
            var retryOptions = new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = portalOptions.RetryMaxAttempts,
                Delay = TimeSpan.FromSeconds(portalOptions.RetryBaseDelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldRetryAfterHeader = true,
            };
            // POST محصول نباید در لایهٔ HTTP تکرار شود؛ idempotency پنل نمی‌تواند
            // دو تلاش داخلی یک فراخوانی را در پرتالِ فاقد idempotency key تشخیص دهد.
            retryOptions.DisableForUnsafeHttpMethods();
            pipeline
                .AddRetry(retryOptions)
                .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = portalOptions.CircuitBreakerMinimumThroughput,
                    SamplingDuration = TimeSpan.FromSeconds(portalOptions.CircuitBreakerSamplingDurationSeconds),
                    BreakDuration = TimeSpan.FromSeconds(portalOptions.CircuitBreakerBreakDurationSeconds),
                })
                .AddTimeout(TimeSpan.FromSeconds(portalOptions.AttemptTimeoutSeconds));
        });

        // Registration order matters: resilience wraps these handlers, therefore
        // every retry acquires its own global rate-limit permit (ADR-026).
        httpClient.AddHttpMessageHandler<PortalRateLimitingHandler>();
        httpClient.AddHttpMessageHandler<PortalRequestHeadersHandler>();

        return httpClient;
    }
}
