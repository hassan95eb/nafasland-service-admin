using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NafasLand.Admin.Shared.Infrastructure.Auditing;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Kernel.Auditing;
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
///
/// ADR-048: this is also the one place a Denied AuditLog row is written (for an
/// IAuditableCommand rejected for lack of permission — either branch below) —
/// AuditBehavior itself never sees a denied command, since the exception is
/// thrown before the pipeline reaches it. The "not authenticated" and
/// "password change required" rejections above are deliberately not logged
/// here; ADR-048 only names the two permission-shaped branches.
/// </summary>
internal sealed class AuthorizationBehavior<TCommand, TResponse>(
    IAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor,
    IAuditLogWriter auditLogWriter,
    ICorrelationIdAccessor correlationIdAccessor)
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
                    await WriteDeniedIfAuditableAsync(command, cancellationToken);
                    throw new AuthorizationDeniedException(
                        $"دسترسی {requiresPermission.RequiredPermission} لازم است.");
                }

                break;

            case IRequiresAuthenticatedUser:
                break;

            default:
                await WriteDeniedIfAuditableAsync(command, cancellationToken);
                throw new AuthorizationDeniedException(
                    $"روی {typeof(TCommand).Name} هیچ permission ای تعریف نشده؛ طبق پیش‌فرض بسته رد شد.");
        }

        return await next();
    }

    private Task WriteDeniedIfAuditableAsync(TCommand command, CancellationToken cancellationToken)
    {
        if (command is not IAuditableCommand auditable)
        {
            return Task.CompletedTask;
        }

        // The handler never ran, so IAuditContext was never filled: EntityId and
        // Before/AfterJson stay null on a Denied record — expected, not a bug.
        var (actorUserId, actorRoleAtTime, ipAddress, userAgent) = AuditActorInfo.Extract(httpContextAccessor.HttpContext);

        var entry = new AuditLogEntry(
            CorrelationId: correlationIdAccessor.CorrelationId,
            ActorUserId: actorUserId,
            OnBehalfOfUserId: null,
            ActorRoleAtTime: actorRoleAtTime,
            Action: auditable.AuditAction,
            EntityType: auditable.AuditEntityType,
            EntityId: null,
            BeforeJson: null,
            AfterJson: null,
            ChangedFields: null,
            Outcome: AuditOutcome.Denied,
            UpstreamStatus: null,
            FailureReason: null,
            IpAddress: ipAddress,
            UserAgent: userAgent);

        return auditLogWriter.WriteAsync(entry, cancellationToken);
    }
}
