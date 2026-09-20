using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Features.CreateUser;

internal sealed record CreateUserCommand(string Username, string InitialPassword)
    : ICommand<CreateUserResult>, IRequiresPermission, IAuditableCommand
{
    public string RequiredPermission => IdentityPermissions.UsersManage;
    public string AuditAction => "UserCreated";
    public string AuditEntityType => "User";
}

internal sealed record CreateUserResult(Guid Id, string Username);
