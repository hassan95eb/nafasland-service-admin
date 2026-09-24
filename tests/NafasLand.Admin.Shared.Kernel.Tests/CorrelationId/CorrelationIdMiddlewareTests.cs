using Microsoft.AspNetCore.Http;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;

namespace NafasLand.Admin.Shared.Kernel.Tests.CorrelationId;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task شناسهٔ_معتبر_ورودی_حفظ_می‌شود()
    {
        var correlationId = await RunWithHeaderAsync("7ee1841729f6448d8b99236c4fcb071d");

        Assert.Equal("7ee1841729f6448d8b99236c4fcb071d", correlationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("a:b")]
    [InlineData("<script>")]
    public async Task شناسهٔ_ورودی_نامعتبر_جایگزین_می‌شود(string incoming)
    {
        var correlationId = await RunWithHeaderAsync(incoming);

        Assert.NotEqual(incoming, correlationId);
        Assert.Equal(32, correlationId.Length);
    }

    [Fact]
    public async Task شناسهٔ_ورودی_طولانی‌تر_از_سقف_جایگزین_می‌شود()
    {
        var incoming = new string('a', CorrelationIdMiddleware.MaxIncomingLength + 1);

        var correlationId = await RunWithHeaderAsync(incoming);

        Assert.NotEqual(incoming, correlationId);
        Assert.True(correlationId.Length <= CorrelationIdMiddleware.MaxIncomingLength);
    }

    private static async Task<string> RunWithHeaderAsync(string headerValue)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = headerValue;
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        return (string)context.Items[CorrelationIdMiddleware.ItemsKey]!;
    }
}
