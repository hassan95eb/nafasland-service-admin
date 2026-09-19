using NafasLand.Admin.Modules.Identity.Security;

namespace NafasLand.Admin.Modules.Identity.Tests.Security;

public sealed class Argon2PasswordHasherTests
{
    [Fact]
    public void رمز_درست_تأیید_می‌شود()
    {
        var hasher = new Argon2PasswordHasher();
        var hash = hasher.Hash("Correct-Horse-Battery-Staple-1");

        Assert.True(hasher.Verify("Correct-Horse-Battery-Staple-1", hash));
    }

    [Fact]
    public void رمز_غلط_رد_می‌شود()
    {
        var hasher = new Argon2PasswordHasher();
        var hash = hasher.Hash("Correct-Horse-Battery-Staple-1");

        Assert.False(hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void دو_بار_هش‌کردن_همان_رمز_دو_رشتهٔ_متفاوت_می‌دهد()
    {
        var hasher = new Argon2PasswordHasher();

        var firstHash = hasher.Hash("same-password");
        var secondHash = hasher.Hash("same-password");

        Assert.NotEqual(firstHash, secondHash);
        Assert.True(hasher.Verify("same-password", firstHash));
        Assert.True(hasher.Verify("same-password", secondHash));
    }
}
