namespace NafasLand.Admin.Shared.Kernel.Permissions;

/// <summary>
/// A command implementing this interface only needs a signed-in user, with no
/// specific permission (e.g. Logout, ChangePassword — every user is allowed to run
/// them on their own account). This is deliberately a different marker from
/// <see cref="IRequiresPermission"/>, not a variant of it: a command with no
/// permission key at all must still be rejected under the default-closed rule
/// unless it explicitly opts into one of these two markers.
/// </summary>
public interface IRequiresAuthenticatedUser
{
}
