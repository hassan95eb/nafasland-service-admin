namespace NafasLand.Admin.Shared.Kernel.Errors;

/// <summary>
/// Login failed because of a wrong username/password, or ChangePassword was
/// called with the wrong current password. Maps to 401. The message is
/// deliberately generic ("username or password is wrong") so it never confirms
/// whether the username itself exists.
/// </summary>
public sealed class InvalidCredentialsException(string message) : Exception(message)
{
}
