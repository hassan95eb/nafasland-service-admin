using System.ComponentModel.DataAnnotations;

namespace NafasLand.Admin.Shared.Infrastructure.Configuration;

/// <summary>
/// One connection string for the whole panel, shared by all modules; isolation is
/// done with schemas, not with a separate database or connection (ADR-019).
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required(AllowEmptyStrings = false, ErrorMessage = "رشتهٔ اتصال پایگاه داده تعریف نشده است.")]
    public string ConnectionString { get; init; } = string.Empty;
}
