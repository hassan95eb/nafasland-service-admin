using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NafasLand.Admin.Shared.Infrastructure.Authorization;

/// <summary>
/// Temporary stand-in for the full Identity model, which is step 1's job. Builds a
/// fake user with a fixed id who is always authenticated; its permissions are read
/// from the <c>X-Test-Permissions</c> header (a comma-separated string) so both the
/// allowed and the rejected path can be exercised on the same endpoint.
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
