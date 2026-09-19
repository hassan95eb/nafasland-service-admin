using System.ComponentModel.DataAnnotations;

namespace NafasLand.Admin.Modules.Identity.Configuration;

/// <summary>
/// Credentials for the first SuperAdmin account (ADR-022), read from config only
/// — never hardcoded. Bound and validated at startup (ADR-039) inside
/// IdentityModule.RegisterServices; missing/invalid values mean the app does not
/// start (enforced by ValidateOnStart, since Api cannot reach this internal type
/// to force validation explicitly the way it does for DatabaseOptions/PortalOptions).
/// </summary>
internal sealed class SuperAdminSeedOptions
{
    public const string SectionName = "Identity:SuperAdmin";

    [Required(AllowEmptyStrings = false, ErrorMessage = "نام کاربری سوپرادمین تعریف نشده است.")]
    public string Username { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "رمز عبور اولیهٔ سوپرادمین تعریف نشده است.")]
    public string Password { get; init; } = string.Empty;
}
