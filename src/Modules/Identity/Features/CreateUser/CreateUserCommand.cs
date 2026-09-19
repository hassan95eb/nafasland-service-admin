using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Features.CreateUser;

internal sealed record CreateUserCommand(string Username, string InitialPassword)
    : ICommand<CreateUserResult>, IRequiresPermission
{
    public string RequiredPermission => IdentityPermissions.UsersManage;
}

internal sealed record CreateUserResult(Guid Id, string Username);
