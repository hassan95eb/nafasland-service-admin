using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Sample.Features.Ping;

internal sealed record PingCommand(string Message) : ICommand<PingResult>, IRequiresPermission
{
    public string RequiredPermission => SamplePermissions.Ping;
}

internal sealed record PingResult(Guid Id, string Message, DateTime CreatedAtUtc);
