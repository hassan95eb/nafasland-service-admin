using System.ComponentModel.DataAnnotations;

namespace NafasLand.Admin.Api.Configuration;

/// <summary>
/// Deliberately not named the same as the standard "Logging" section (which is
/// for dotnet's default LogLevel providers); this section only reads Serilog's
/// log file settings (ADR-042).
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
