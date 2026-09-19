using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Shared.Infrastructure.ErrorHandling;

/// <summary>
/// همهٔ خطاهای مدیریت‌نشده را به ProblemDetails همراه با correlationId
/// map می‌کند (ADR-036). پیام خام استثناهای پیش‌بینی‌نشده هرگز به کاربر
/// نشان داده نمی‌شود؛ فقط در لاگ می‌ماند.
///
/// AddExceptionHandler&lt;T&gt; این کلاس را Singleton ثبت می‌کند، پس
/// ICorrelationIdAccessor (که Scoped است) از سازنده تزریق نمی‌شود؛ به‌جایش
/// هر بار از RequestServices همان درخواست خوانده می‌شود.
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
            CommandValidationException validation => BuildValidationProblemDetails(validation),
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "خطای غیرمنتظره",
                Detail = "خطایی در پردازش درخواست رخ داد. لطفاً با شمارهٔ پیگیری با پشتیبانی تماس بگیرید.",
            },
        };

        problemDetails.Extensions["correlationId"] = correlationId;

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "خطای مدیریت‌نشده");
        }
        else
        {
            logger.LogWarning(exception, "درخواست رد شد: {Title}", problemDetails.Title);
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        // به‌صورت object سریالایز می‌شود، نه ProblemDetails؛ وگرنه System.Text.Json
        // فقط اعضای نوع اعلان‌شده (ProblemDetails) را می‌نویسد و فیلد errors ی که
        // فقط روی ValidationProblemDetails است، از پاسخ حذف می‌شود.
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
