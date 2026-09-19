namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// Extracts the module name from a command's namespace (e.g. from
/// <c>NafasLand.Admin.Modules.Sample.Features.Ping</c> the result is
/// <c>Sample</c>). TransactionBehavior uses this name as the key to resolve the
/// correct <see cref="Kernel.Persistence.IUnitOfWork"/>, so it stays unambiguous
/// once several modules have their own DbContext.
/// </summary>
internal static class ModuleNameResolver
{
    private const string ModuleNamespacePrefix = "NafasLand.Admin.Modules.";

    public static string? ResolveFromNamespace(string? commandNamespace)
    {
        if (commandNamespace is null || !commandNamespace.StartsWith(ModuleNamespacePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var rest = commandNamespace[ModuleNamespacePrefix.Length..];
        var dotIndex = rest.IndexOf('.');
        return dotIndex < 0 ? rest : rest[..dotIndex];
    }
}
