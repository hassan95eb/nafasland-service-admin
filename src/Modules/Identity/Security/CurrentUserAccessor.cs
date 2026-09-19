using System.Security.Claims;

namespace NafasLand.Admin.Modules.Identity.Security;

/// <summary>Reads the current user's id out of the NameIdentifier claim set at sign-in (LoginCommandHandler).</summary>
internal static class CurrentUserAccessor
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("کاربر جاری شناسه ندارد.");
        return Guid.Parse(claim.Value);
    }
}
