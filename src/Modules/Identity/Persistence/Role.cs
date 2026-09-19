using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Identity.Persistence;

internal sealed class Role
{
    private readonly List<RolePermission> _rolePermissions = [];

    private Role()
    {
        Name = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    /// <summary>True only for SuperAdmin: SetRolePermissions rejects edits to it (ADR-021), since the next permission sync overwrites them anyway.</summary>
    public bool IsSystemManaged { get; private set; }

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions;

    public static Role Create(string name, bool isSystemManaged)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsSystemManaged = isSystemManaged,
        };
    }

    /// <summary>ADR-021: a system-managed role (SuperAdmin) can't be hand-edited — the next permission sync would just overwrite it.</summary>
    public void EnsureEditable()
    {
        if (IsSystemManaged)
        {
            throw new ConflictException(
                "نقش SuperAdmin به‌صورت خودکار مدیریت می‌شود؛ دستکاری دستی‌اش با همگام‌سازی بعدی permission بازنویسی می‌شود.");
        }
    }
}
