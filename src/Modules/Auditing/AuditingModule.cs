using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Auditing.Configuration;
using NafasLand.Admin.Modules.Auditing.Contracts;
using NafasLand.Admin.Modules.Auditing.Features.ExportAuditLog;
using NafasLand.Admin.Modules.Auditing.Features.Queries;
using NafasLand.Admin.Modules.Auditing.Jobs;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Configuration;
using NafasLand.Admin.Shared.Infrastructure.Persistence;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Modules;
using NafasLand.Admin.Shared.Kernel.Persistence;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Auditing;

/// <summary>Fills in step 0's AuditBehavior skeleton with a real, append-only AuditLog (ADR-009, ADR-048).</summary>
internal sealed class AuditingModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuditingDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(
                databaseOptions.ConnectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", AuditingDbContext.SchemaName));
        });

        services.AddKeyedScoped<IUnitOfWork>(
            "Auditing",
            (serviceProvider, _) => serviceProvider.GetRequiredService<AuditingDbContext>());

        services.AddScoped<IMigrationCheck, EfCoreMigrationCheck<AuditingDbContext>>();

        services.AddHealthChecks()
            .AddCheck<EfCoreDatabaseHealthCheck<AuditingDbContext>>("audit-database");

        services.AddOptions<AuditExportOptions>()
            .Bind(configuration.GetSection(AuditExportOptions.SectionName));

        // Not keyed, unlike IUnitOfWork — exactly one AuditLog table for the
        // whole application; this registration (added after AddSharedInfrastructure's
        // NullAuditLogWriter default) is the one that actually wins resolution.
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();

        services.AddScoped<AuditExportBackgroundJob>();
        services.AddScoped<AuditLogPurgeJob>();
        services.AddScoped<IAuditingBootstrapper, AuditingBootstrapper>();

        services.AddScoped<IValidator<ExportAuditLogCommand>, ExportAuditLogCommandValidator>();
        services.AddScoped<ICommandHandler<ExportAuditLogCommand, ExportAuditLogResult>, ExportAuditLogCommandHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        ListAuditLogsEndpoint.Map(app);
        GetAuditLogEndpoint.Map(app);
        GetUserActivityEndpoint.Map(app);
        GetProductAuditHistoryEndpoint.Map(app);
        GetExportStatusEndpoint.Map(app);
        DownloadExportEndpoint.Map(app);
        ExportAuditLogEndpoint.Map(app);
    }

    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        new PermissionDefinition(AuditingPermissions.ReadAll, "مشاهدهٔ گزارش فعالیت", IsSuperAdminOnly: true),
        new PermissionDefinition(AuditingPermissions.Export, "خروجی گزارش فعالیت", IsSuperAdminOnly: true),
    ];
}
