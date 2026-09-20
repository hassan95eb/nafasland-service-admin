using System.ComponentModel.DataAnnotations;

namespace NafasLand.Admin.Modules.Catalog.Contracts.Configuration;

public sealed class PortalOptions
{
    public const string SectionName = "Portal";

    [Required(AllowEmptyStrings = false, ErrorMessage = "آدرس پایهٔ پرتال تعریف نشده است.")]
    [Url(ErrorMessage = "آدرس پایهٔ پرتال معتبر نیست.")]
    public string BaseUrl { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "توکن پرتال تعریف نشده است (ADR-039).")]
    public string BearerToken { get; init; } = string.Empty;

    [Range(0.1, 2.0, ErrorMessage = "نرخ درخواست باید بین ۰.۱ تا ۲ در ثانیه باشد (ADR-026).")]
    public double RateLimitPerSecond { get; init; } = 1.5;

    [Range(0, 1000, ErrorMessage = "ظرفیت صف محدودکنندهٔ نرخ باید بین صفر تا ۱۰۰۰ باشد.")]
    public int RateLimitQueueCapacity { get; init; } = 20;

    [Range(0.1, 300, ErrorMessage = "مهلت انتظار صف باید بین ۰.۱ تا ۳۰۰ ثانیه باشد.")]
    public double RateLimitQueueTimeoutSeconds { get; init; } = 15;

    [Range(1, 10, ErrorMessage = "تعداد تلاش مجدد باید بین ۱ تا ۱۰ باشد.")]
    public int RetryMaxAttempts { get; init; } = 3;

    [Range(0.01, 30, ErrorMessage = "تأخیر پایهٔ تلاش مجدد باید بین ۰.۰۱ تا ۳۰ ثانیه باشد.")]
    public double RetryBaseDelaySeconds { get; init; } = 0.5;

    [Range(2, 100, ErrorMessage = "حداقل توان عملیاتی circuit breaker باید بین ۲ تا ۱۰۰ باشد.")]
    public int CircuitBreakerMinimumThroughput { get; init; } = 4;

    [Range(2, 300, ErrorMessage = "پنجرهٔ circuit breaker باید بین ۲ تا ۳۰۰ ثانیه باشد.")]
    public double CircuitBreakerSamplingDurationSeconds { get; init; } = 30;

    [Range(0.5, 300, ErrorMessage = "مدت باز ماندن circuit breaker باید بین ۰.۵ تا ۳۰۰ ثانیه باشد.")]
    public double CircuitBreakerBreakDurationSeconds { get; init; } = 30;

    [Range(0.1, 120, ErrorMessage = "مهلت هر تلاش باید بین ۰.۱ تا ۱۲۰ ثانیه باشد.")]
    public double AttemptTimeoutSeconds { get; init; } = 10;

    [Required(AllowEmptyStrings = false, ErrorMessage = "شناسهٔ محصول تستی تعریف نشده است (ADR-029).")]
    public string TestProductId { get; init; } = string.Empty;
}
