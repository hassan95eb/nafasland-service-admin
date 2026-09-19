namespace NafasLand.Admin.Shared.Kernel.Errors;

/// <summary>
/// The command conflicts with the current state of the target resource — e.g.
/// SetRolePermissions on a role with <c>IsSystemManaged = true</c>. Maps to 409,
/// distinct from both the 403 of AuthorizationDeniedException and the 400 of
/// CommandValidationException.
/// </summary>
public sealed class ConflictException(string message) : Exception(message)
{
}
