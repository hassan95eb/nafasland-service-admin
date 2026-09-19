using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Contracts;

/// <summary>
/// One permission declared by one module's <c>IModule.Permissions</c>, paired
/// with the module's own name. Api pairs these up right after module discovery
/// (the module name comes from the discovered assembly, ADR-005) and hands the
/// full list to <see cref="IIdentityBootstrapper"/> — this is how Identity learns
/// a permission's owning module without PermissionDefinition itself carrying it
/// (adding that field would have forced a change to every existing module,
/// including Sample, which this step must not touch).
/// </summary>
public sealed record ModulePermissionDefinition(string ModuleName, PermissionDefinition Definition);

/// <summary>
/// Startup-only entry point (called once from Api, after the pending-migrations
/// check passes and before the app starts serving traffic): synchronizes the
/// Permission table from every enabled module's declarations, ensures the
/// SuperAdmin/Admin roles exist, keeps SuperAdmin filled with every known
/// permission, and seeds the first SuperAdmin account (ADR-005, ADR-021,
/// ADR-022). Exposed via Contracts so Api never references Identity's internal
/// persistence types.
/// </summary>
public interface IIdentityBootstrapper
{
    Task BootstrapAsync(IReadOnlyList<ModulePermissionDefinition> allPermissions, CancellationToken cancellationToken);
}
