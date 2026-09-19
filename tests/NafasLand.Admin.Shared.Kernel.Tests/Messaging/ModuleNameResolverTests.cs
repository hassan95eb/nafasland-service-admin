using NafasLand.Admin.Shared.Infrastructure.Messaging;

namespace NafasLand.Admin.Shared.Kernel.Tests.Messaging;

public sealed class ModuleNameResolverTests
{
    [Theory]
    [InlineData("NafasLand.Admin.Modules.Sample.Features.Ping", "Sample")]
    [InlineData("NafasLand.Admin.Modules.Identity", "Identity")]
    public void ResolveFromNamespace_با_namespace_ماژول_نام_ماژول_را_برمی‌گرداند(string ns, string expected)
    {
        var result = ModuleNameResolver.ResolveFromNamespace(ns);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NafasLand.Admin.Shared.Kernel.Tests")]
    [InlineData("SomeOther.Namespace")]
    public void ResolveFromNamespace_با_namespace_غیر_ماژول_null_برمی‌گرداند(string? ns)
    {
        var result = ModuleNameResolver.ResolveFromNamespace(ns);

        Assert.Null(result);
    }
}
