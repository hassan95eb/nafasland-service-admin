using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Infrastructure.ErrorHandling;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// زیرساخت مشترک: CorrelationId، Authorization Policy مبتنی بر permission،
    /// و pipeline اجباری با ترتیب دقیق ADR-006. ترتیب ثبت زیر همان ترتیب
    /// اجراست: Logging → Validation → Authorization → Transaction → Audit.
    /// </summary>
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdAccessor, HttpCorrelationIdAccessor>();
        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        services.AddProblemDetails();

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();

        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

        return services;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// باید بعد از <c>UseAuthentication</c> فراخوانی شود (ADR-042).
    /// </summary>
    public static IApplicationBuilder UseUserContextLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<UserContextLoggingMiddleware>();
    }
}
