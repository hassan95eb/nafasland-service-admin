namespace NafasLand.Admin.Shared.Kernel.Errors;

/// <summary>
/// Login was attempted on a deactivated account (ADR-023). Deliberately a
/// different message from <see cref="AccountLockedException"/> so the two are
/// distinguishable by the client.
/// </summary>
public sealed class AccountInactiveException(string message) : Exception(message)
{
}
