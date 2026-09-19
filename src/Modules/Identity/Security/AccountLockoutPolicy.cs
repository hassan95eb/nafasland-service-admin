namespace NafasLand.Admin.Modules.Identity.Security;

/// <summary>Fixed lockout policy (ADR-023): 5 consecutive failed attempts → 15-minute lock.</summary>
internal static class AccountLockoutPolicy
{
    public const int MaxFailedAttempts = 5;

    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
}
