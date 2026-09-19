using System.ComponentModel.DataAnnotations;

namespace NafasLand.Admin.Shared.Infrastructure.Configuration;

/// <summary>
/// یک رشتهٔ اتصال برای کل پنل، مشترک بین همهٔ ماژول‌ها؛ جداسازی با schema
/// انجام می‌شود، نه با دیتابیس یا اتصال جدا (ADR-019).
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required(AllowEmptyStrings = false, ErrorMessage = "رشتهٔ اتصال پایگاه داده تعریف نشده است.")]
    public string ConnectionString { get; init; } = string.Empty;
}
