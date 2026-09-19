using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// سومین حلقهٔ pipeline (ADR-006). پیش‌فرض بسته: commandی که
/// <see cref="IRequiresPermission"/> را پیاده نکرده باشد رد می‌شود، حتی اگر
/// کاربر هر permission ای داشته باشد. چک واقعی از طریق Authorization Policy و
/// IAuthorizationHandler انجام می‌شود (ADR-001)، نه با خواندن مستقیم claim.
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
