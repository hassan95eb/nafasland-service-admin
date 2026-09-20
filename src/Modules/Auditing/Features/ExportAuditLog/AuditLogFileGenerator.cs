using System.Text;
using ClosedXML.Excel;
using NafasLand.Admin.Modules.Auditing.Persistence;

namespace NafasLand.Admin.Modules.Auditing.Features.ExportAuditLog;

/// <summary>
/// The flat export view is deliberately narrower than a full AuditLog row —
/// BeforeJson/AfterJson are large blobs meant for the single-record detail view
/// (GetAuditLogEndpoint), not a row-per-record spreadsheet; ChangedFields is
/// included instead as a readable summary of what changed.
/// </summary>
internal static class AuditLogFileGenerator
{
    private static readonly string[] Columns =
    [
        "Id", "CreatedAt", "ActorUserId", "ActorRoleAtTime", "Action",
        "EntityType", "EntityId", "Outcome", "ChangedFields", "FailureReason",
        "IpAddress", "UserAgent",
    ];

    public static (byte[] Bytes, string ContentType, string FileName) Generate(IReadOnlyList<AuditLog> rows, string format)
    {
        return format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            ? GenerateXlsx(rows)
            : GenerateCsv(rows);
    }

    private static (byte[], string, string) GenerateCsv(IReadOnlyList<AuditLog> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', Columns));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',', GetValues(row).Select(EscapeCsvField)));
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        return (bytes, "text/csv", $"audit-log-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private static (byte[], string, string) GenerateXlsx(IReadOnlyList<AuditLog> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("AuditLog");

        for (var columnIndex = 0; columnIndex < Columns.Length; columnIndex++)
        {
            worksheet.Cell(1, columnIndex + 1).Value = Columns[columnIndex];
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            var values = GetValues(row);
            for (var columnIndex = 0; columnIndex < values.Length; columnIndex++)
            {
                worksheet.Cell(rowIndex, columnIndex + 1).Value = values[columnIndex];
            }

            rowIndex++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"audit-log-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    private static string[] GetValues(AuditLog row) =>
    [
        row.Id.ToString(),
        row.CreatedAt.ToString("O"),
        row.ActorUserId?.ToString() ?? string.Empty,
        row.ActorRoleAtTime,
        row.Action,
        row.EntityType ?? string.Empty,
        row.EntityId ?? string.Empty,
        row.Outcome.ToString(),
        row.ChangedFields ?? string.Empty,
        row.FailureReason ?? string.Empty,
        row.IpAddress ?? string.Empty,
        row.UserAgent ?? string.Empty,
    ];

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
