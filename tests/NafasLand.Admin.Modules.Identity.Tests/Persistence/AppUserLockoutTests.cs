using NafasLand.Admin.Modules.Identity.Persistence;
using NafasLand.Admin.Modules.Identity.Security;

namespace NafasLand.Admin.Modules.Identity.Tests.Persistence;

/// <summary>ADR-023: 5 consecutive failed attempts lock the account for 15 minutes; a successful login resets the counter. Uses plain DateTimeOffset values (an injected "now"), never Thread.Sleep.</summary>
public sealed class AppUserLockoutTests
{
    private static AppUser CreateUser(DateTimeOffset now) =>
        AppUser.Create("ali", "hash", "Argon2id", now, mustChangePassword: false, isProtected: false, createdByUserId: null);

    [Fact]
    public void بعد_از_کمتر_از_۵_تلاش_ناموفق_قفل_نمی‌شود()
    {
        var now = DateTimeOffset.UtcNow;
        var user = CreateUser(now);

        for (var i = 0; i < AccountLockoutPolicy.MaxFailedAttempts - 1; i++)
        {
            user.RegisterFailedLogin(now, AccountLockoutPolicy.MaxFailedAttempts, AccountLockoutPolicy.LockDuration);
        }

        Assert.False(user.IsLocked(now));
    }

    [Fact]
    public void دقیقاً_در_پنجمین_تلاش_ناموفق_پیاپی_قفل_می‌شود()
    {
        var now = DateTimeOffset.UtcNow;
        var user = CreateUser(now);

        for (var i = 0; i < AccountLockoutPolicy.MaxFailedAttempts; i++)
        {
            user.RegisterFailedLogin(now, AccountLockoutPolicy.MaxFailedAttempts, AccountLockoutPolicy.LockDuration);
        }

        Assert.True(user.IsLocked(now));
    }

    [Fact]
    public void بعد_از_گذشت_مدت_قفل_باز_می‌شود()
    {
        var now = DateTimeOffset.UtcNow;
        var user = CreateUser(now);

        for (var i = 0; i < AccountLockoutPolicy.MaxFailedAttempts; i++)
        {
            user.RegisterFailedLogin(now, AccountLockoutPolicy.MaxFailedAttempts, AccountLockoutPolicy.LockDuration);
        }

        var afterLockExpires = now.Add(AccountLockoutPolicy.LockDuration).AddSeconds(1);

        Assert.False(user.IsLocked(afterLockExpires));
    }

    [Fact]
    public void ورود_موفق_شمارنده_و_قفل_را_صفر_می‌کند()
    {
        var now = DateTimeOffset.UtcNow;
        var user = CreateUser(now);

        for (var i = 0; i < AccountLockoutPolicy.MaxFailedAttempts; i++)
        {
            user.RegisterFailedLogin(now, AccountLockoutPolicy.MaxFailedAttempts, AccountLockoutPolicy.LockDuration);
        }

        user.RegisterSuccessfulLogin();

        Assert.False(user.IsLocked(now));
        Assert.Equal(0, user.FailedLoginCount);
    }
}
