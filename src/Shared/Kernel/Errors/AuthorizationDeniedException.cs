namespace NafasLand.Admin.Shared.Kernel.Errors;

/// <summary>
/// A command with no permission defined, or a user lacking that permission, was
/// rejected (default-closed, ADR-006). Maps to 403 with ProblemDetails.
/// </summary>
public sealed class AuthorizationDeniedException(string message) : Exception(message)
{
}
