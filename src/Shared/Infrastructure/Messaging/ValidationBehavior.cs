using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Infrastructure.Auditing;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Infrastructure.Messaging;

/// <summary>
/// The second link in the pipeline (ADR-006). If no validator is registered for a
/// command, it passes through without error; not every command necessarily has a
/// validation rule.
/// </summary>
internal sealed class ValidationBehavior<TCommand, TResponse>(IServiceProvider serviceProvider)
    : IPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validator = serviceProvider.GetService<IValidator<TCommand>>();
        if (validator is null)
        {
            return await next();
        }

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).ToArray());

            await WriteFailedIfAuditableAsync(command, cancellationToken);
            throw new CommandValidationException(errors);
        }

        return await next();
    }

    private Task WriteFailedIfAuditableAsync(TCommand command, CancellationToken cancellationToken)
    {
        if (command is not IAuditableCommand auditable)
        {
            return Task.CompletedTask;
        }

        var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        var auditLogWriter = serviceProvider.GetService<IAuditLogWriter>();
        var correlationIdAccessor = serviceProvider.GetService<ICorrelationIdAccessor>();
        if (httpContextAccessor is null || auditLogWriter is null || correlationIdAccessor is null)
        {
            return Task.CompletedTask;
        }

        var (actorUserId, actorRoleAtTime, ipAddress, userAgent) = AuditActorInfo.Extract(httpContextAccessor.HttpContext);
        return auditLogWriter.WriteAsync(new AuditLogEntry(
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
            Outcome: AuditOutcome.Failed,
            UpstreamStatus: null,
            FailureReason: "ValidationFailed",
            IpAddress: ipAddress,
            UserAgent: userAgent), cancellationToken);
    }
}
