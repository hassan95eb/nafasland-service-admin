using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Modules.Catalog.Features.Queries;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Kernel.Modules;
using NafasLand.Admin.Shared.Kernel.Permissions;
using Polly;

namespace NafasLand.Admin.Modules.Catalog;

internal sealed class CatalogModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var portalOptions = configuration.GetSection(PortalOptions.SectionName).Get<PortalOptions>()
            ?? new PortalOptions();

        services.AddMemoryCache();
        services.AddSingleton<PortalRateLimiter>();
        services.AddTransient<PortalRateLimitingHandler>();
        services.AddSingleton<ProductListCache>();
        services.AddSingleton<IPortalTokenProvider, PortalTokenProvider>();

        var httpClient = services.AddHttpClient<IPortalProductClient, PortalProductClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PortalOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + '/', UriKind.Absolute);
        });
        httpClient.RedactLoggedHeaders(headerName =>
            string.Equals(headerName, "Authorization", StringComparison.OrdinalIgnoreCase));

        httpClient.AddResilienceHandler("portal-read", pipeline =>
        {
            pipeline
                .AddRetry(new Microsoft.Extensions.Http.Resilience.HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = portalOptions.RetryMaxAttempts,
                    Delay = TimeSpan.FromSeconds(portalOptions.RetryBaseDelaySeconds),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldRetryAfterHeader = true,
                })
                .AddCircuitBreaker(new Microsoft.Extensions.Http.Resilience.HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = portalOptions.CircuitBreakerMinimumThroughput,
                    SamplingDuration = TimeSpan.FromSeconds(portalOptions.CircuitBreakerSamplingDurationSeconds),
                    BreakDuration = TimeSpan.FromSeconds(portalOptions.CircuitBreakerBreakDurationSeconds),
                })
                .AddTimeout(TimeSpan.FromSeconds(portalOptions.AttemptTimeoutSeconds));
        });

        // Registration order matters: resilience wraps this handler, therefore
        // every retry acquires its own global rate-limit permit (ADR-026).
        httpClient.AddHttpMessageHandler<PortalRateLimitingHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        ListProductsEndpoint.Map(app);
        GetProductEndpoint.Map(app);
    }

    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        new PermissionDefinition(CatalogPermissions.ProductsRead, "مشاهدهٔ محصولات"),
    ];
}
