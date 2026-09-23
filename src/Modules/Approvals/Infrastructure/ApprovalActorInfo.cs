using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Approvals.Infrastructure;

/// <summary>
/// A small, module-local equivalent of Shared.Infrastructure's own
/// AuditActorInfo. That helper is internal to the Shared.Infrastructure
/// assembly and cannot be referenced from here (ADR-004 keeps `internal`
/// meaningful across assemblies too) — Approvals commands do not go through
/// AuditBehavior at all (see the note on OnBehalfOfUserId in the Features
/// handlers), so they need their own actor extraction.
/// </summary>
internal static class ApprovalActorInfo
{
    public static Guid RequireUserId(HttpContext? httpContext)
    {
        var idClaim = httpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (idClaim is not null && Guid.TryParse(idClaim.Value, out var userId))
        {
            return userId;
        }

        throw new AuthorizationDeniedException("کاربر احراز هویت‌نشده.");
    }

    public static string RoleAtTime(HttpContext? httpContext)
    {
        var roleNames = httpContext?.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList() ?? [];
        return roleNames.Count > 0 ? string.Join(", ", roleNames) : "نامشخص";
    }

    public static (string? IpAddress, string? UserAgent) RequestInfo(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return (null, null);
        }

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgentValue = httpContext.Request.Headers.UserAgent.ToString();
        var userAgent = string.IsNullOrEmpty(userAgentValue) ? null : userAgentValue;
        return (ipAddress, userAgent);
    }
}
