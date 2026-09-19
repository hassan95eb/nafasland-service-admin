namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// نام ماژول را از namespace یک command استخراج می‌کند (مثلاً از
/// <c>NafasLand.Admin.Modules.Sample.Features.Ping</c> نتیجه‌اش <c>Sample</c>
/// است). TransactionBehavior از این نام به‌عنوان کلید برای resolve کردن
/// <see cref="Kernel.Persistence.IUnitOfWork"/> صحیح استفاده می‌کند تا با
/// وجود چند ماژول دارای DbContext مبهم نشود.
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
