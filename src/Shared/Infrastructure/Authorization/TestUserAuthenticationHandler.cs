using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NafasLand.Admin.Shared.Infrastructure.Authorization;

/// <summary>
/// جایگزین موقت مدل کامل Identity که کار گام ۱ است. یک کاربر ساختگی با
/// شناسهٔ ثابت می‌سازد که همیشه احراز هویت شده است؛ permissionهایش از هدر
/// <c>X-Test-Permissions</c> (رشتهٔ جداشده با کاما) خوانده می‌شود تا بشود هم
/// مسیر مجاز و هم مسیر رد‌شده را روی همان endpoint امتحان کرد.
/// </summary>
public sealed class TestUserAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestUser";
    public const string PermissionsHeaderName = "X-Test-Permissions";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "test-user") };

        if (Request.Headers.TryGetValue(PermissionsHeaderName, out var permissionsHeader))
        {
            claims.AddRange(permissionsHeader
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(permission => new Claim(PermissionClaimTypes.Permission, permission)));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
