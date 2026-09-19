using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace NafasLand.Admin.Shared.Infrastructure.Auditing;

/// <summary>
/// Shared by AuditBehavior and AuthorizationBehavior so the actor fields (ADR-009)
/// are read from HttpContext in exactly one place. "System" (per the AuditLog
/// data model) is reserved for the background jobs that call IAuditLogWriter
/// directly, outside any HTTP request — this helper is only used from within the
/// pipeline, where a request always exists, so it never needs to produce it.
/// </summary>
internal static class AuditActorInfo
{
    public static (Guid? ActorUserId, string ActorRoleAtTime, string? IpAddress, string? UserAgent) Extract(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return (null, "نامشخص", null, null);
        }

        var user = httpContext.User;

        Guid? actorUserId = null;
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        if (idClaim is not null && Guid.TryParse(idClaim.Value, out var parsedId))
        {
            actorUserId = parsedId;
        }

        // A user can hold several roles at once (ADR-003); AuditLog is a
        // reporting/display concern, not an authorization check, so joining all
        // of them here does not conflict with ADR-001's "never check by role name".
        var roleNames = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList();
        var actorRoleAtTime = roleNames.Count > 0 ? string.Join(", ", roleNames) : "نامشخص";

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgentValue = httpContext.Request.Headers.UserAgent.ToString();
        var userAgent = string.IsNullOrEmpty(userAgentValue) ? null : userAgentValue;

        return (actorUserId, actorRoleAtTime, ipAddress, userAgent);
    }
}
