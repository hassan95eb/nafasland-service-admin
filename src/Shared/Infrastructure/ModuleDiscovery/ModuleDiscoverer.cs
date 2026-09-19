using System.Reflection;
using Microsoft.Extensions.Configuration;
using NafasLand.Admin.Shared.Kernel.Modules;

namespace NafasLand.Admin.Shared.Infrastructure.ModuleDiscovery;

/// <summary>
/// اسکن اسمبلی‌های کنار Api برای پیدا کردن پیاده‌سازی‌های <see cref="IModule"/>
/// (ADR-005). ماژول از روی نام اسمبلی‌اش (پس از پیشوند) کشف می‌شود، پس
/// افزودن ماژول جدید فقط اضافه‌کردن یک پروژه است، نه ویرایش Program.cs.
/// فلگ فیچر هر ماژول با کلید <c>Modules:&lt;نام&gt;:Enabled</c> در کانفیگ
/// کنترل می‌شود (ADR-012)؛ ماژول خاموش اصلاً رجیستر نمی‌شود.
/// این کلاس قبل از build شدن سرویس‌ها اجرا می‌شود، پس عمداً به هیچ لاگری
/// وابسته نیست؛ خلاصهٔ نتیجه در <see cref="ModuleDiscoveryResult"/> برمی‌گردد
/// تا میزبان بعد از build آن را لاگ کند.
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
