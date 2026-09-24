using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Returns.Approvals;
using NafasLand.Admin.Modules.Returns.Features.Queries;
using NafasLand.Admin.Modules.Returns.Infrastructure;
using NafasLand.Admin.Modules.Returns.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Configuration;
using NafasLand.Admin.Shared.Infrastructure.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Portal;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Modules;
using NafasLand.Admin.Shared.Kernel.Persistence;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Returns;

/// <summary>
/// Returns (ADR-054): reads one order from the portal and, after a SuperAdmin
/// approves, stores a return in its own schema. It never writes to the portal
/// and depends only on Shared/Kernel — the approval flow is reached through
/// the keyed IApprovalExecutor, never by referencing the Approvals module.
/// </summary>
internal sealed class ReturnsModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ReturnsDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(
                databaseOptions.ConnectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", ReturnsDbContext.SchemaName));
        });

        services.AddScoped<IMigrationCheck, EfCoreMigrationCheck<ReturnsDbContext>>();
        services.AddHealthChecks()
            .AddCheck<EfCoreDatabaseHealthCheck<ReturnsDbContext>>("returns-database");

        services.AddScoped<IValidator<RegisterReturnPayload>, RegisterReturnPayloadValidator>();
        services.AddKeyedScoped<IApprovalExecutor, RegisterReturnApprovalExecutor>(RegisterReturnApprovalExecutor.RequestTypeKey);

        services.AddPortalHttpClient<IPortalOrderClient, PortalOrderClient>(configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        GetOrderPreviewEndpoint.Map(app);
        ListReturnsEndpoints.Map(app);
    }

    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        new PermissionDefinition(ReturnsPermissions.Request, "ثبت درخواست مرجوعی"),
        new PermissionDefinition(ReturnsPermissions.Review, "تأیید یا رد مرجوعی", IsSuperAdminOnly: true),
        new PermissionDefinition(ReturnsPermissions.ReadAll, "مشاهدهٔ همهٔ مرجوعی‌ها", IsSuperAdminOnly: true),
    ];
}
