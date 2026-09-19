using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Infrastructure.ModuleDiscovery;

namespace NafasLand.Admin.Shared.Infrastructure.Extensions;

public static class ModuleHostExtensions
{
    /// <summary>
    /// همهٔ ماژول‌های فعال را کشف و <see cref="IModule.RegisterServices"/> هرکدام
    /// را فرا می‌خواند (ADR-005). نتیجه را برمی‌گرداند تا هم
    /// <see cref="MapModuleEndpoints"/> بعداً همان نمونه‌ها را map کند و هم
    /// میزبان بتواند خلاصهٔ کشف را (بعد از build) لاگ کند.
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
