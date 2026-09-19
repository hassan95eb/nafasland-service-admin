using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Identity.Features.Logout;

internal sealed class LogoutCommandHandler(IHttpContextAccessor httpContextAccessor)
    : ICommandHandler<LogoutCommand, LogoutResult>
{
    public async Task<LogoutResult> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        await httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return new LogoutResult();
    }
}
