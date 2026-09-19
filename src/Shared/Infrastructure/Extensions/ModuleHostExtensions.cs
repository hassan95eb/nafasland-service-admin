using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Infrastructure.ModuleDiscovery;

namespace NafasLand.Admin.Shared.Infrastructure.Extensions;

public static class ModuleHostExtensions
{
    /// <summary>
    /// Discovers every enabled module and calls each one's
    /// <see cref="IModule.RegisterServices"/> (ADR-005). Returns the result so
    /// <see cref="MapModuleEndpoints"/> can later map the same instances, and so
    /// the host can log a discovery summary (after build).
    /// </summary>
    public static ModuleDiscoveryResult AddModules(this IServiceCollection services, IConfiguration configuration)
    {
        var discovery = ModuleDiscoverer.DiscoverEnabledModules(configuration);

        foreach (var discovered in discovery.EnabledModules)
        {
            discovered.Module.RegisterServices(services, configuration);
        }

        return discovery;
    }

    public static void MapModuleEndpoints(this IEndpointRouteBuilder app, IReadOnlyList<DiscoveredModule> modules)
    {
        foreach (var discovered in modules)
        {
            discovered.Module.MapEndpoints(app);
        }
    }
}
