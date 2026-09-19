using System.ComponentModel.DataAnnotations;

namespace NafasLand.Admin.Api.Configuration;

/// <summary>
/// جای‌گیر تنظیمات پرتال طبق ADR-039/ADR-012: باید از همین گام تعریف و در
/// استارتاپ اعتبارسنجی شود، حتی پیش از آنکه ماژول Catalog (گام ۳) واقعاً از
/// آن استفاده کند. هیچ تماسی با پرتال در این گام انجام نمی‌شود.
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
