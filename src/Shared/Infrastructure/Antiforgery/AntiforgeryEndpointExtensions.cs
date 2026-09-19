using Microsoft.AspNetCore.Builder;

namespace NafasLand.Admin.Shared.Infrastructure.Antiforgery;

/// <summary>
/// ASP.NET Core's own antiforgery integration (UseAntiforgery/DisableAntiforgery)
/// only auto-validates minimal-API endpoints that bind from a form; a plain JSON
/// body endpoint is not covered, confirmed by testing against this app's own
/// endpoints. AntiforgeryValidationMiddleware below does the actual, explicit
/// validation for every mutating request; this marker is its one opt-out, used
/// only by LoginCommand's endpoint (no session cookie exists yet at login time).
/// </summary>
public static class AntiforgeryEndpointExtensions
{
    internal sealed class SkipAntiforgeryValidationMarker
    {
    }

    public static TBuilder SkipAntiforgeryValidation<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(new SkipAntiforgeryValidationMarker());
        return builder;
    }
}
