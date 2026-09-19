using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Features.ChangePassword;

internal sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword)
    : ICommand<ChangePasswordResult>, IRequiresAuthenticatedUser, IAllowedWhenPasswordChangeRequired;

internal sealed record ChangePasswordResult;
