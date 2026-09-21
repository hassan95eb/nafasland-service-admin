using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NafasLand.Admin.Shared.Infrastructure.Antiforgery;
using NafasLand.Admin.Shared.Infrastructure.Auditing;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Infrastructure.ErrorHandling;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Shared infrastructure: CorrelationId, a permission-based Authorization
    /// Policy, and the mandatory pipeline with the exact ADR-006 order. The
    /// registration order below is the same as the execution order: Logging →
    /// Validation → Authorization → Idempotency → Transaction → Audit.
    /// </summary>
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdAccessor, HttpCorrelationIdAccessor>();
        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        services.AddProblemDetails();

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();

        // Same scoped instance behind both types (ADR-048): a Handler writes
        // through the narrow Kernel interface, AuditBehavior reads the
        // accumulated state back off the concrete type.
        services.AddScoped<AuditContext>();
        services.AddScoped<IAuditContext>(sp => sp.GetRequiredService<AuditContext>());

        // Safe default so the pipeline resolves even without the Auditing module
        // (tests, or that module disabled via feature flag) — AuditingModule
        // registers the real writer afterwards, which then wins resolution.
        services.TryAddScoped<IAuditLogWriter, NullAuditLogWriter>();

        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

        return services;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Must be called after <c>UseAuthentication</c> (ADR-042).
    /// </summary>
    public static IApplicationBuilder UseUserContextLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<UserContextLoggingMiddleware>();
    }

    /// <summary>
    /// Must be called after routing has resolved an endpoint (i.e. after any
    /// explicit UseRouting, or simply after UseAuthentication/UseAuthorization in
    /// minimal hosting) so AntiforgeryValidationMiddleware can read endpoint
    /// metadata via HttpContext.GetEndpoint().
    /// </summary>
    public static IApplicationBuilder UseAntiforgeryValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AntiforgeryValidationMiddleware>();
    }
}
