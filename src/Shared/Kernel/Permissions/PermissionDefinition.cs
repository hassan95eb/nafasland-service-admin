namespace NafasLand.Admin.Shared.Kernel.Permissions;

/// <summary>
/// Declaration of a permission by the module that owns it (ADR-001, ADR-005). The
/// key follows the <c>&lt;resource&gt;.&lt;action&gt;</c> format and is defined as a
/// const inside the module itself. <paramref name="IsSuperAdminOnly"/> (ADR-002)
/// defaults to <see langword="false"/> so existing declarations (e.g. Sample's)
/// keep compiling unchanged.
/// </summary>
public sealed record PermissionDefinition(string Key, string DisplayName, bool IsSuperAdminOnly = false);

/// <summary>
/// A command implementing this interface declares which permission it requires.
/// A command that does not implement this interface is rejected by
/// AuthorizationBehavior under the default-closed rule (ADR-006).
/// </summary>
public interface IRequiresPermission
{
    string RequiredPermission { get; }
}
