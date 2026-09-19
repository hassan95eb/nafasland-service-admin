using NafasLand.Admin.Modules.Sample.Features.Ping;

namespace NafasLand.Admin.Modules.Sample.Tests.Features.Ping;

public sealed class PingCommandValidatorTests
{
    private readonly PingCommandValidator _validator = new();

    [Fact]
    public void پیام_خالی_نامعتبر_است()
    {
        var result = _validator.Validate(new PingCommand(""));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void پیام_بیش_از_۲۰۰_نویسه_نامعتبر_است()
    {
        var result = _validator.Validate(new PingCommand(new string('پ', 201)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void پیام_معتبر_قبول_می‌شود()
    {
        var result = _validator.Validate(new PingCommand("سلام"));

        Assert.True(result.IsValid);
    }
}
