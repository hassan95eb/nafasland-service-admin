namespace NafasLand.Admin.Shared.Kernel.Errors;

/// <summary>
/// Login was attempted while the account is locked out after repeated failed
/// attempts (ADR-023). Maps to 423 Locked; deliberately a different message from
/// <see cref="AccountInactiveException"/> so the two are distinguishable.
/// </summary>
public sealed class AccountLockedException(string message) : Exception(message)
{
}
