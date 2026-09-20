namespace NafasLand.Admin.Modules.Identity.Persistence;

internal sealed class Permission
{
    private Permission()
    {
        Key = string.Empty;
        DisplayName = string.Empty;
        ModuleName = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; }

    public string DisplayName { get; private set; }

    public string ModuleName { get; private set; }

    public bool IsSuperAdminOnly { get; private set; }

    public static Permission Create(string key, string displayName, string moduleName, bool isSuperAdminOnly)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Key = key,
            DisplayName = displayName,
            ModuleName = moduleName,
            IsSuperAdminOnly = isSuperAdminOnly,
        };
    }

    /// <summary>Upsert path (ADR-005): a permission's module or SuperAdminOnly flag may change between deploys; the row is updated in place, never replaced.</summary>
    public void SyncFrom(string displayName, string moduleName, bool isSuperAdminOnly)
    {
        DisplayName = displayName;
        ModuleName = moduleName;
        IsSuperAdminOnly = isSuperAdminOnly;
    }
}
