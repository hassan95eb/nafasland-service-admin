namespace NafasLand.Admin.Shared.Kernel.Errors;

/// <summary>
/// The user's MustChangePassword flag is set, and the requested command is not
/// one of the two allowed while that is true (ChangePassword, Logout). Maps to a
/// distinct ProblemDetails (errorCode PASSWORD_CHANGE_REQUIRED), not a plain
/// permission 403, so a client can tell the two apart (ADR-023).
/// </summary>
public sealed class PasswordChangeRequiredException(string message) : Exception(message)
{
}
