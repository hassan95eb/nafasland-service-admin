using System.ComponentModel.DataAnnotations;

namespace NafasLand.Admin.Api.Configuration;

/// <summary>
/// عمداً هم‌نام بخش استاندارد «Logging» نیست (که برای LogLevel providerهای
/// پیش‌فرض dotnet است)؛ این بخش فقط تنظیمات فایل لاگ Serilog را می‌خواند
/// (ADR-042).
/// </summary>
public sealed class LoggingOptions
{
    public const string SectionName = "LogFile";

    [Required(AllowEmptyStrings = false, ErrorMessage = "پوشهٔ لاگ تعریف نشده است.")]
    public string Directory { get; init; } = "logs";

    [Required(AllowEmptyStrings = false, ErrorMessage = "سطح حداقل لاگ تعریف نشده است.")]
    [RegularExpression(
        "^(Verbose|Debug|Information|Warning|Error|Fatal)$",
        ErrorMessage = "سطح لاگ باید یکی از Verbose/Debug/Information/Warning/Error/Fatal باشد.")]
    public string MinimumLevel { get; init; } = "Information";
}
