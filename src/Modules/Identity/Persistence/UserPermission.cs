namespace NafasLand.Admin.Modules.Identity.Persistence;

/// <summary>Direct per-user override on top of role permissions (ADR-021). Deny always wins.</summary>
internal enum PermissionEffect
{
    Grant = 0,
    Deny = 1,
}

internal sealed class UserPermission
{
    private UserPermission()
    {
    }

    public UserPermission(Guid userId, Guid permissionId, PermissionEffect effect)
    {
        UserId = userId;
        PermissionId = permissionId;
        Effect = effect;
    }

    public Guid UserId { get; private set; }

    public Guid PermissionId { get; private set; }

    public PermissionEffect Effect { get; private set; }

    public AppUser? User { get; private set; }

    public Permission? Permission { get; private set; }

    public void SetEffect(PermissionEffect effect) => Effect = effect;
}
