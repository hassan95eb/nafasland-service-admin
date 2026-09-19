using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using NafasLand.Admin.Shared.Kernel.Errors;
using AntiforgeryValidationException = Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException;

namespace NafasLand.Admin.Shared.Infrastructure.Antiforgery;

/// <summary>
/// Explicit, global antiforgery check for every mutating request (ADR-013):
/// every non-GET/HEAD/OPTIONS/TRACE endpoint requires a valid antiforgery token
/// unless marked with SkipAntiforgeryValidation (only LoginCommand's endpoint).
/// Applies module-wide, including Sample's endpoints, without needing to touch
/// their code — this step must not modify the Sample module.
/// </summary>
internal sealed class AntiforgeryValidationMiddleware(RequestDelegate next, IAntiforgery antiforgery)
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get, HttpMethods.Head, HttpMethods.Options, HttpMethods.Trace,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var skipValidation = context.GetEndpoint()?.Metadata
            .GetMetadata<AntiforgeryEndpointExtensions.SkipAntiforgeryValidationMarker>() is not null;

        if (skipValidation || SafeMethods.Contains(context.Request.Method))
        {
            await next(context);
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException exception)
        {
            throw new AntiforgeryValidationFailedException(exception.Message);
        }

        await next(context);
    }
}
