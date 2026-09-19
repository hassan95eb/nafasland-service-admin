using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Shared.Infrastructure.ErrorHandling;

/// <summary>
/// Maps every unhandled exception to ProblemDetails along with the correlationId
/// (ADR-036). The raw message of an unexpected exception is never shown to the
/// user; it only stays in the log.
///
/// AddExceptionHandler&lt;T&gt; registers this class as a Singleton, so
/// ICorrelationIdAccessor (which is Scoped) is not injected via the constructor;
/// instead it is read from that request's RequestServices each time.
/// </summary>
internal sealed class ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.RequestServices.GetRequiredService<ICorrelationIdAccessor>().CorrelationId;

        var problemDetails = exception switch
        {
            AuthorizationDeniedException authorizationDenied => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "دسترسی غیرمجاز",
                Detail = authorizationDenied.Message,
            },
            PasswordChangeRequiredException passwordChangeRequired => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "نیاز به تغییر رمز عبور",
                Detail = passwordChangeRequired.Message,
            },
            AccountLockedException accountLocked => new ProblemDetails
            {
                Status = StatusCodes.Status423Locked,
                Title = "حساب قفل است",
                Detail = accountLocked.Message,
            },
            AccountInactiveException accountInactive => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "حساب غیرفعال است",
                Detail = accountInactive.Message,
            },
            InvalidCredentialsException invalidCredentials => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "ورود ناموفق",
                Detail = invalidCredentials.Message,
            },
            ConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "تداخل با وضعیت فعلی",
                Detail = conflict.Message,
            },
            AntiforgeryValidationFailedException antiforgeryFailed => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "توکن antiforgery نامعتبر یا موجود نیست",
                Detail = antiforgeryFailed.Message,
            },
            CommandValidationException validation => BuildValidationProblemDetails(validation),
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "خطای غیرمنتظره",
                Detail = "خطایی در پردازش درخواست رخ داد. لطفاً با شمارهٔ پیگیری با پشتیبانی تماس بگیرید.",
            },
        };

        problemDetails.Extensions["correlationId"] = correlationId;

        if (exception is PasswordChangeRequiredException)
        {
            problemDetails.Extensions["errorCode"] = "PASSWORD_CHANGE_REQUIRED";
        }

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "خطای مدیریت‌نشده");
        }
        else
        {
            logger.LogWarning(exception, "درخواست رد شد: {Title}", problemDetails.Title);
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        // Serialized as object, not ProblemDetails; otherwise System.Text.Json
        // only writes the members of the declared type (ProblemDetails), and the
        // errors field, which only exists on ValidationProblemDetails, would be
        // dropped from the response.
        await httpContext.Response.WriteAsJsonAsync((object)problemDetails, cancellationToken);
        return true;
    }

    private static ValidationProblemDetails BuildValidationProblemDetails(CommandValidationException validation)
    {
        return new ValidationProblemDetails(new Dictionary<string, string[]>(validation.Errors))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "خطای اعتبارسنجی",
            Detail = validation.Message,
        };
    }
}
