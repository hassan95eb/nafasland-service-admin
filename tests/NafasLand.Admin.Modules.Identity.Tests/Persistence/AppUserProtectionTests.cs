using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Identity.Tests.Persistence;

/// <summary>ToggleUserActive and SetUserRoles both call AppUser.EnsureNotProtected() first; the required "reject destructive op on IsProtected user" test, at the entity level.</summary>
public sealed class AppUserProtectionTests
{
    private static AppUser CreateUser(bool isProtected) =>
        AppUser.Create("superadmin", "hash", "Argon2id", DateTimeOffset.UtcNow, mustChangePassword: true, isProtected, createdByUserId: null);

    [Fact]
    public void حساب_محافظت‌شده_اجازهٔ_عملیات_مخرب_نمی‌دهد()
    {
        var protectedUser = CreateUser(isProtected: true);

        Assert.Throws<AuthorizationDeniedException>(() => protectedUser.EnsureNotProtected("غیرفعال‌سازی"));
    }

    [Fact]
    public void حساب_معمولی_اجازهٔ_عملیات_مخرب_می‌دهد()
    {
        var normalUser = CreateUser(isProtected: false);

        var exception = Record.Exception(() => normalUser.EnsureNotProtected("غیرفعال‌سازی"));

        Assert.Null(exception);
    }
}
