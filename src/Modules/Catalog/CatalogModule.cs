using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Modules.Catalog.Features.Queries;
using NafasLand.Admin.Modules.Catalog.Features.CreateProduct;
using NafasLand.Admin.Modules.Catalog.Features.UpdateProduct;
using NafasLand.Admin.Modules.Catalog.Features.UpdateVariantPriceAndInventory;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Modules.Catalog.Jobs;
using NafasLand.Admin.Modules.Catalog.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Configuration;
using NafasLand.Admin.Shared.Infrastructure.Persistence;
using NafasLand.Admin.Shared.Kernel.Idempotency;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Modules;
using NafasLand.Admin.Shared.Kernel.Persistence;
using NafasLand.Admin.Shared.Kernel.Permissions;
using Polly;

namespace NafasLand.Admin.Modules.Catalog;

internal sealed class CatalogModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var portalOptions = configuration.GetSection(PortalOptions.SectionName).Get<PortalOptions>()
            ?? new PortalOptions();

        services.AddDbContext<CatalogDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(
                databaseOptions.ConnectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.SchemaName));
        });

        services.AddKeyedScoped<IUnitOfWork>(
            "Catalog",
            (serviceProvider, _) => serviceProvider.GetRequiredService<CatalogDbContext>());
        services.AddScoped<IIdempotencyStore, CatalogIdempotencyStore>();
        services.AddScoped<IMigrationCheck, EfCoreMigrationCheck<CatalogDbContext>>();
        services.AddHealthChecks()
            .AddCheck<EfCoreDatabaseHealthCheck<CatalogDbContext>>("catalog-database");

        services.AddMemoryCache();
        services.AddSingleton<PortalRateLimiter>();
        services.AddTransient<PortalRateLimitingHandler>();
        services.AddSingleton<ProductCache>();
        services.AddSingleton<TaxonomyCache>();
        services.AddScoped<IProductHtmlSanitizer, ProductHtmlSanitizer>();
        services.AddSingleton<IPortalTokenProvider, PortalTokenProvider>();
        services.AddScoped<IdempotencyPurgeJob>();
        services.AddScoped<ICatalogBootstrapper, CatalogBootstrapper>();
        services.AddScoped<IValidator<UpdateVariantPriceAndInventoryCommand>, UpdateVariantPriceAndInventoryCommandValidator>();
        services.AddScoped<ICommandHandler<UpdateVariantPriceAndInventoryCommand, UpdateVariantPriceAndInventoryResult>,
            UpdateVariantPriceAndInventoryCommandHandler>();
        services.AddScoped<IValidator<CreateProductCommand>, CreateProductCommandValidator>();
        services.AddScoped<ICommandHandler<CreateProductCommand, CreateProductResult>, CreateProductCommandHandler>();
        services.AddScoped<IValidator<UpdateProductCommand>, UpdateProductCommandValidator>();
        services.AddScoped<ICommandHandler<UpdateProductCommand, UpdateProductResult>, UpdateProductCommandHandler>();

        var httpClient = services.AddHttpClient<IPortalProductClient, PortalProductClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PortalOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + '/', UriKind.Absolute);
        });
        httpClient.RedactLoggedHeaders(headerName =>
            string.Equals(headerName, "Authorization", StringComparison.OrdinalIgnoreCase));

        httpClient.AddResilienceHandler("portal-read", pipeline =>
        {
            var retryOptions = new Microsoft.Extensions.Http.Resilience.HttpRetryStrategyOptions
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
        GetProductTaxonomyEndpoints.Map(app);
        CreateProductEndpoint.Map(app);
        UpdateProductEndpoint.Map(app);
        UpdateVariantPriceAndInventoryEndpoint.Map(app);
    }

    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        new PermissionDefinition(CatalogPermissions.ProductsRead, "مشاهدهٔ محصولات"),
        new PermissionDefinition(CatalogPermissions.ProductsWrite, "ایجاد و ویرایش محصولات"),
    ];
}
