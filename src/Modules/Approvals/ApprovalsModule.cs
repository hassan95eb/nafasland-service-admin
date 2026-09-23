using FluentValidation;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Approvals.Contracts;
using NafasLand.Admin.Modules.Approvals.Features.ApproveApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Features.CancelApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Features.CreateApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Features.Queries;
using NafasLand.Admin.Modules.Approvals.Features.RejectApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Features.RetryApprovalRequest;
using NafasLand.Admin.Modules.Approvals.Infrastructure;
using NafasLand.Admin.Modules.Approvals.Jobs;
using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Configuration;
using NafasLand.Admin.Shared.Infrastructure.Persistence;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Modules;
using NafasLand.Admin.Shared.Kernel.Persistence;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Approvals;

/// <summary>
/// The generic approval-flow module (ADR-010). Knows nothing about Catalog or
/// any other domain — it only dispatches to whichever IApprovalExecutor is
/// registered (by the owning module, keyed by RequestType) for a request's
/// RequestType.
/// </summary>
internal sealed class ApprovalsModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApprovalsDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(
                databaseOptions.ConnectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", ApprovalsDbContext.SchemaName));
        });

        services.AddKeyedScoped<IUnitOfWork>(
            "Approvals",
            (serviceProvider, _) => serviceProvider.GetRequiredService<ApprovalsDbContext>());

        services.AddScoped<IMigrationCheck, EfCoreMigrationCheck<ApprovalsDbContext>>();
        services.AddHealthChecks()
            .AddCheck<EfCoreDatabaseHealthCheck<ApprovalsDbContext>>("approvals-database");

        services.AddScoped<IApprovalExecutorRegistry, ApprovalExecutorRegistry>();
        services.AddScoped<ApprovalExecutionCoordinator>();
        services.AddScoped<ApprovalExpiryJob>();
        services.AddScoped<IApprovalsBootstrapper, ApprovalsBootstrapper>();

        services.AddScoped<IValidator<CreateApprovalRequestCommand>, CreateApprovalRequestCommandValidator>();
        services.AddScoped<ICommandHandler<CreateApprovalRequestCommand, CreateApprovalRequestResult>, CreateApprovalRequestCommandHandler>();

        services.AddScoped<ICommandHandler<ApproveApprovalRequestCommand, ApprovalDecisionResult>, ApproveApprovalRequestCommandHandler>();

        services.AddScoped<IValidator<RejectApprovalRequestCommand>, RejectApprovalRequestCommandValidator>();
        services.AddScoped<ICommandHandler<RejectApprovalRequestCommand, ApprovalDecisionResult>, RejectApprovalRequestCommandHandler>();

        services.AddScoped<ICommandHandler<CancelApprovalRequestCommand, ApprovalDecisionResult>, CancelApprovalRequestCommandHandler>();

        services.AddScoped<ICommandHandler<RetryApprovalRequestCommand, ApprovalDecisionResult>, RetryApprovalRequestCommandHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        CreateApprovalRequestEndpoint.Map(app);
        ListApprovalRequestsEndpoint.Map(app);
        GetApprovalRequestEndpoint.Map(app);
        ApproveApprovalRequestEndpoint.Map(app);
        RejectApprovalRequestEndpoint.Map(app);
        CancelApprovalRequestEndpoint.Map(app);
        RetryApprovalRequestEndpoint.Map(app);
    }

    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        new PermissionDefinition(ApprovalsPermissions.Review, "تأیید یا رد درخواست‌ها", IsSuperAdminOnly: true),
        new PermissionDefinition(ApprovalsPermissions.ReadAll, "مشاهدهٔ کارتابل کامل درخواست‌ها", IsSuperAdminOnly: true),
    ];
}
