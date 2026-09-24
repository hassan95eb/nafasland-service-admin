using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Approvals;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Features.Queries;
using NafasLand.Admin.Modules.Catalog.Features.CreateProduct;
using NafasLand.Admin.Modules.Catalog.Features.UpdateProduct;
using NafasLand.Admin.Modules.Catalog.Features.UpdateVariantPriceAndInventory;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Modules.Catalog.Jobs;
using NafasLand.Admin.Modules.Catalog.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Configuration;
using NafasLand.Admin.Shared.Infrastructure.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Portal;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Idempotency;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Modules;
using NafasLand.Admin.Shared.Kernel.Persistence;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Catalog;

internal sealed class CatalogModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
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
        services.AddSingleton<ProductCache>();
        services.AddSingleton<TaxonomyCache>();
        services.AddScoped<IProductHtmlSanitizer, ProductHtmlSanitizer>();
        services.AddScoped<IdempotencyPurgeJob>();
        services.AddScoped<ICatalogBootstrapper, CatalogBootstrapper>();
        services.AddScoped<IValidator<UpdateVariantPriceAndInventoryCommand>, UpdateVariantPriceAndInventoryCommandValidator>();
        services.AddScoped<ICommandHandler<UpdateVariantPriceAndInventoryCommand, UpdateVariantPriceAndInventoryResult>,
            UpdateVariantPriceAndInventoryCommandHandler>();
        services.AddScoped<IValidator<CreateProductCommand>, CreateProductCommandValidator>();
        services.AddScoped<ICommandHandler<CreateProductCommand, CreateProductResult>, CreateProductCommandHandler>();
        services.AddScoped<IValidator<UpdateProductCommand>, UpdateProductCommandValidator>();
        services.AddScoped<ICommandHandler<UpdateProductCommand, UpdateProductResult>, UpdateProductCommandHandler>();

        // Approvals executors (ADR-010/030/031/032): registered keyed by their own
        // RequestType, resolved by the Approvals module without either module
        // referencing the other (ADR-004) — see IApprovalExecutor's own comment.
        services.AddKeyedScoped<IApprovalExecutor, DeleteProductApprovalExecutor>(DeleteProductApprovalExecutor.RequestTypeKey);
        services.AddKeyedScoped<IApprovalExecutor, PublishProductApprovalExecutor>(PublishProductApprovalExecutor.RequestTypeKey);
        services.AddKeyedScoped<IApprovalExecutor, ProductStatusApprovalExecutor>(ProductStatusApprovalExecutor.RequestTypeKey);
        services.AddKeyedScoped<IApprovalExecutor, DeleteVariantApprovalExecutor>(DeleteVariantApprovalExecutor.RequestTypeKey);

        services.AddPortalHttpClient<IPortalProductClient, PortalProductClient>(configuration);
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
        new PermissionDefinition(CatalogPermissions.ProductsDeleteRequest, "درخواست حذف محصول"),
        new PermissionDefinition(CatalogPermissions.ProductsDelete, "تأیید حذف محصول", IsSuperAdminOnly: true),
        new PermissionDefinition(CatalogPermissions.ProductsPublishRequest, "درخواست انتشار یا لغو انتشار محصول"),
        new PermissionDefinition(CatalogPermissions.ProductsPublish, "تأیید انتشار یا لغو انتشار محصول", IsSuperAdminOnly: true),
        new PermissionDefinition(CatalogPermissions.ProductsStatusRequest, "درخواست تغییر ویژه/پرفروش‌ترین"),
        new PermissionDefinition(CatalogPermissions.ProductsStatus, "تأیید تغییر ویژه/پرفروش‌ترین", IsSuperAdminOnly: true),
        new PermissionDefinition(CatalogPermissions.VariantsDeleteRequest, "درخواست حذف واریانت"),
        new PermissionDefinition(CatalogPermissions.VariantsDelete, "تأیید حذف واریانت", IsSuperAdminOnly: true),
    ];
}
