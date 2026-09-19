namespace NafasLand.Admin.Shared.Kernel.Errors;

/// <summary>
/// A mutating request had no valid antiforgery token (ADR-013). Maps to 400,
/// separate from the 401/403 of authentication/authorization.
/// </summary>
public sealed class AntiforgeryValidationFailedException(string message) : Exception(message)
{
}
