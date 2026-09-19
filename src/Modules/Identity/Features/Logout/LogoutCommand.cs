using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Features.Logout;

internal sealed record LogoutCommand : ICommand<LogoutResult>, IRequiresAuthenticatedUser, IAllowedWhenPasswordChangeRequired;

internal sealed record LogoutResult;
