using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// The third link in the pipeline (ADR-006). Default-closed: a command that does
/// not implement <see cref="IRequiresPermission"/> is rejected, no matter what
/// permissions the user has. The actual check goes through an Authorization
/// Policy and IAuthorizationHandler (ADR-001), not by reading the claim directly.
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
        if (command is not IRequiresPermission requiresPermission)
        {
            throw new AuthorizationDeniedException(
                $"روی {typeof(TCommand).Name} هیچ permission ای تعریف نشده؛ طبق پیش‌فرض بسته رد شد.");
        }

        var user = httpContextAccessor.HttpContext?.User
            ?? throw new AuthorizationDeniedException("کاربر احراز هویت‌نشده.");

        var result = await authorizationService.AuthorizeAsync(
            user,
            resource: null,
            new PermissionRequirement(requiresPermission.RequiredPermission));

        if (!result.Succeeded)
        {
            throw new AuthorizationDeniedException(
                $"دسترسی {requiresPermission.RequiredPermission} لازم است.");
        }

        return await next();
    }
}
