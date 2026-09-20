using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Sample.Features.Ping;

/// <summary>
/// IAuditableCommand here is step 2's optional, explicitly-sanctioned
/// proof-of-mechanism (its own prompt: "فقط PingCommand ... برای اثبات") — not a
/// real business need for the Sample module, which stays otherwise untouched.
/// </summary>
internal sealed record PingCommand(string Message) : ICommand<PingResult>, IRequiresPermission, IAuditableCommand
{
    public string RequiredPermission => SamplePermissions.Ping;

    public string AuditAction => "PingSent";

    public string AuditEntityType => "PingRecord";
}

internal sealed record PingResult(Guid Id, string Message, DateTime CreatedAtUtc);
