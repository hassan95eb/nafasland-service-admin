using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace NafasLand.Admin.Modules.Approvals.Tests.Fakes;

internal static class TestHttpContext
{
    public static HttpContext Create(Guid userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, "Test");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }
}

internal sealed class FakeHttpContextAccessor(HttpContext httpContext) : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; } = httpContext;
}
