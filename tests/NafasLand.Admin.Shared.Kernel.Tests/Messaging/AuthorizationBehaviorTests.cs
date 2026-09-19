using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Permissions;

namespace NafasLand.Admin.Shared.Kernel.Tests.Messaging;

public sealed class AuthorizationBehaviorTests
{
    private sealed record ProtectedCommand(string RequiredPermission) : ICommand<string>, IRequiresPermission;

    private sealed record UnprotectedCommand : ICommand<string>;

    private static ServiceProvider BuildProvider(ClaimsPrincipal user)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();

        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = user } };
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal BuildUser(params string[] permissions)
    {
        var claims = permissions.Select(p => new Claim(PermissionClaimTypes.Permission, p));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public async Task command_بدون_IRequiresPermission_همیشه_رد_می‌شود()
    {
        var provider = BuildProvider(BuildUser("anything"));
        var behavior = provider.GetRequiredService<IPipelineBehavior<UnprotectedCommand, string>>();

        await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            behavior.HandleAsync(new UnprotectedCommand(), () => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task command_با_permission_و_کاربر_دارای_همان_permission_اجرا_می‌شود()
    {
        var provider = BuildProvider(BuildUser("sample.ping"));
        var behavior = provider.GetRequiredService<IPipelineBehavior<ProtectedCommand, string>>();

        var result = await behavior.HandleAsync(
            new ProtectedCommand("sample.ping"),
            () => Task.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task command_با_permission_و_کاربر_بدون_آن_permission_رد_می‌شود()
    {
        var provider = BuildProvider(BuildUser("some.other.permission"));
        var behavior = provider.GetRequiredService<IPipelineBehavior<ProtectedCommand, string>>();

        await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            behavior.HandleAsync(new ProtectedCommand("sample.ping"), () => Task.FromResult("ok"), CancellationToken.None));
    }
}
