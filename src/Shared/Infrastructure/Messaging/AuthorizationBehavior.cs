using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// The third link in the pipeline (ADR-006). Default-closed: a command must
/// implement exactly one of <see cref="IAllowAnonymousCommand"/> (skips every
/// check below — only LoginCommand), <see cref="IRequiresPermission"/> (checked
/// through an Authorization Policy and IAuthorizationHandler, ADR-001) or
/// <see cref="IRequiresAuthenticatedUser"/> (signed in, no specific permission).
/// A command with none of these is rejected no matter what permissions the user
/// has. The forced-password-change gate (ADR-023) runs for every command except
/// the ones marked <see cref="IAllowedWhenPasswordChangeRequired"/>.
/// </summary>
internal sealed class AuthorizationBehavior<TCommand, TResponse>(
    IAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor)
    : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (command is IAllowAnonymousCommand)
        {
            return await next();
        }

        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated is not true)
        {
            throw new AuthorizationDeniedException("کاربر احراز هویت‌نشده.");
        }

        if (command is not IAllowedWhenPasswordChangeRequired
            && user.HasClaim(AccountClaimTypes.MustChangePassword, "true"))
        {
            throw new PasswordChangeRequiredException("پیش از ادامه باید رمز عبور را تغییر دهید.");
        }

        switch (command)
        {
            case IRequiresPermission requiresPermission:
                var result = await authorizationService.AuthorizeAsync(
                    user,
                    resource: null,
                    new PermissionRequirement(requiresPermission.RequiredPermission));

                if (!result.Succeeded)
                {
                    throw new AuthorizationDeniedException(
                        $"دسترسی {requiresPermission.RequiredPermission} لازم است.");
                }

                break;

            case IRequiresAuthenticatedUser:
                break;

            default:
                throw new AuthorizationDeniedException(
                    $"روی {typeof(TCommand).Name} هیچ permission ای تعریف نشده؛ طبق پیش‌فرض بسته رد شد.");
        }

        return await next();
    }
}
