using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FluentValidation;
using NafasLand.Admin.Modules.Sample.Features.Ping;
using NafasLand.Admin.Modules.Sample.Features.UnprotectedPing;
using NafasLand.Admin.Modules.Sample.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Configuration;
using NafasLand.Admin.Shared.Infrastructure.Persistence;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Modules;
using NafasLand.Admin.Shared.Kernel.Persistence;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Sample;

/// <summary>
/// Step 0's sample module: proves that IModule, the pipeline, the database, and
/// migrations all work together. It has no product logic.
/// </summary>
internal sealed class SampleModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SampleDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(
                databaseOptions.ConnectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", SampleDbContext.SchemaName));
        });

        // The key must match the module name in the namespace exactly;
        // ModuleNameResolver extracts this same string from each command's namespace.
        services.AddKeyedScoped<IUnitOfWork>(
            "Sample",
            (serviceProvider, _) => serviceProvider.GetRequiredService<SampleDbContext>());

        services.AddScoped<IMigrationCheck, EfCoreMigrationCheck<SampleDbContext>>();

        services.AddHealthChecks()
            .AddCheck<EfCoreDatabaseHealthCheck<SampleDbContext>>("sample-database");

        services.AddScoped<IValidator<PingCommand>, PingCommandValidator>();
        services.AddScoped<ICommandHandler<PingCommand, PingResult>, PingCommandHandler>();
        services.AddScoped<ICommandHandler<UnprotectedPingCommand, UnprotectedPingResult>, UnprotectedPingCommandHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        PingEndpoint.Map(app);
        UnprotectedPingEndpoint.Map(app);
    }

    public IReadOnlyList<PermissionDefinition> Permissions { get; } =
    [
        new PermissionDefinition(SamplePermissions.Ping, "ارسال پینگ نمونه")
    ];
}
