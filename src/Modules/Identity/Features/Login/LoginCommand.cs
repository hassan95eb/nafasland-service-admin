using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Modules.Identity.Features.Login;

/// <summary>
/// The only command in the whole codebase allowed to implement
/// IAllowAnonymousCommand — it runs before any user is signed in.
/// </summary>
internal sealed record LoginCommand(string Username, string Password) : ICommand<LoginResult>, IAllowAnonymousCommand;

internal sealed record LoginResult(Guid UserId, string Username, bool MustChangePassword, string AntiforgeryToken);
