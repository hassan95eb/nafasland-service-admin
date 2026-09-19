namespace NafasLand.Admin.Modules.Identity.Persistence;

/// <summary>Many-to-many join (ADR-003): a user can hold several roles at once.</summary>
internal sealed class UserRole
{
    private UserRole()
    {
    }

    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public AppUser? User { get; private set; }

    public Role? Role { get; private set; }
}
