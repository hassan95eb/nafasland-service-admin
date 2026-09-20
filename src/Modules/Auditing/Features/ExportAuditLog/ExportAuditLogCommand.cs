using NafasLand.Admin.Modules.Auditing.Features.Queries;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Auditing.Features.ExportAuditLog;

/// <summary>
/// Unlike the four read views, this is a real ICommand — it must be logged
/// itself (ADR-014: "Action = AuditExported"), so it needs the mandatory
/// pipeline (ADR-006), not a plain endpoint.
/// </summary>
internal sealed record ExportAuditLogCommand(AuditLogFilter Filter, string Format)
    : ICommand<ExportAuditLogResult>, IRequiresPermission, IAuditableCommand
{
    public string RequiredPermission => AuditingPermissions.Export;

    public string AuditAction => "AuditExported";

    public string AuditEntityType => "AuditLog";
}

/// <summary>Exactly one of (FileBytes/ContentType/FileName) or JobId is populated, depending on IsAsync.</summary>
internal sealed record ExportAuditLogResult(bool IsAsync, byte[]? FileBytes, string? ContentType, string? FileName, Guid? JobId);
