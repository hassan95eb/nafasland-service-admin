namespace NafasLand.Admin.Modules.Identity.Security;

/// <summary>
/// ADR-023 requires a minimum password length without naming a number; 8 is
/// this module's chosen default (see step-01 report for the assumption).
/// </summary>
internal static class PasswordPolicy
{
    public const int MinLength = 8;
}
