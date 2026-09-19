using System.ComponentModel.DataAnnotations;

namespace NafasLand.Admin.Api.Configuration;

/// <summary>
/// Placeholder for portal settings per ADR-039/ADR-012: must be defined and
/// validated at startup from this step on, even before the Catalog module (step
/// 3) actually uses it. No call to the portal is made at this step.
/// </summary>
public sealed class PortalOptions
{
    public const string SectionName = "Portal";

    [Required(AllowEmptyStrings = false, ErrorMessage = "آدرس پایهٔ پرتال تعریف نشده است.")]
    [Url(ErrorMessage = "آدرس پایهٔ پرتال معتبر نیست.")]
    public string BaseUrl { get; init; } = string.Empty;

    [Range(0.1, 2.0, ErrorMessage = "نرخ درخواست باید بین ۰.۱ تا ۲ در ثانیه باشد (ADR-026).")]
    public double RateLimitPerSecond { get; init; } = 1.5;

    [Required(AllowEmptyStrings = false, ErrorMessage = "شناسهٔ محصول تستی تعریف نشده است (ADR-029).")]
    public string TestProductId { get; init; } = string.Empty;
}
