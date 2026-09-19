using System.Reflection;
using Microsoft.Extensions.Configuration;
using NafasLand.Admin.Shared.Kernel.Modules;

namespace NafasLand.Admin.Shared.Infrastructure.ModuleDiscovery;

/// <summary>
/// Scans the assemblies next to Api to find <see cref="IModule"/> implementations
/// (ADR-005). A module is discovered from its assembly name (after the prefix),
/// so adding a new module is only adding a project, not editing Program.cs. Each
/// module's feature flag is controlled by the <c>Modules:&lt;name&gt;:Enabled</c>
/// config key (ADR-012); a disabled module is never registered at all. This class
/// runs before services are built, so it deliberately does not depend on any
/// logger; the summary is returned in <see cref="ModuleDiscoveryResult"/> so the
/// host can log it after build.
/// </summary>
public static class ModuleDiscoverer
{
    private const string ModuleAssemblyPrefix = "NafasLand.Admin.Modules.";

    public static ModuleDiscoveryResult DiscoverEnabledModules(IConfiguration configuration, string? searchDirectory = null)
    {
        var baseDirectory = searchDirectory ?? AppContext.BaseDirectory;
        var enabled = new List<DiscoveredModule>();
        var disabledModuleNames = new List<string>();

        foreach (var assemblyPath in Directory.GetFiles(baseDirectory, $"{ModuleAssemblyPrefix}*.dll"))
        {
            var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            var moduleName = assemblyName[ModuleAssemblyPrefix.Length..];

            var isEnabled = configuration.GetValue($"Modules:{moduleName}:Enabled", defaultValue: true);
            if (!isEnabled)
            {
                disabledModuleNames.Add(moduleName);
                continue;
            }

            var assembly = Assembly.Load(assemblyName);
            var moduleTypes = assembly.GetTypes()
                .Where(type => typeof(IModule).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface);

            foreach (var moduleType in moduleTypes)
            {
                var instance = (IModule)Activator.CreateInstance(moduleType)!;
                enabled.Add(new DiscoveredModule(moduleName, instance));
            }
        }

        return new ModuleDiscoveryResult(enabled, disabledModuleNames);
    }
}

public sealed record ModuleDiscoveryResult(
    IReadOnlyList<DiscoveredModule> EnabledModules,
    IReadOnlyList<string> DisabledModuleNames);
