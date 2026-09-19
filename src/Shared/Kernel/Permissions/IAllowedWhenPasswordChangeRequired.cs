namespace NafasLand.Admin.Shared.Kernel.Permissions;

/// <summary>
/// Marks a command as the escape valve from the forced-password-change gate
/// (ADR-023): when a user's MustChangePassword flag is set, AuthorizationBehavior
/// rejects every command for that user except the ones implementing this
/// interface (ChangePasswordCommand and LogoutCommand).
/// </summary>
public interface IAllowedWhenPasswordChangeRequired
{
}
