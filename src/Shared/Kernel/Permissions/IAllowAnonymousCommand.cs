namespace NafasLand.Admin.Shared.Kernel.Permissions;

/// <summary>
/// A command implementing this interface skips AuthorizationBehavior entirely,
/// including the authenticated-user check — needed for LoginCommand, which by
/// definition runs before any user is signed in. Only LoginCommand may implement
/// this; every other command, even inside the Identity module, goes through the
/// normal checks.
/// </summary>
public interface IAllowAnonymousCommand
{
}
