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

    private sealed record AnonymousCommand : ICommand<string>, IAllowAnonymousCommand;

    private sealed record AuthenticatedOnlyCommand : ICommand<string>, IRequiresAuthenticatedUser;

    private sealed record GateExemptCommand : ICommand<string>, IRequiresAuthenticatedUser, IAllowedWhenPasswordChangeRequired;

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

    private static ClaimsPrincipal BuildUnauthenticatedUser()
    {
        // No authenticationType passed in -> Identity.IsAuthenticated is false,
        // the same shape a real cookie scheme produces for a missing/invalid cookie.
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    private static ClaimsPrincipal BuildUserRequiringPasswordChange()
    {
        Claim[] claims = [new(AccountClaimTypes.MustChangePassword, "true")];
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

    [Fact]
    public async Task command_با_IAllowAnonymousCommand_حتی_بدون_کاربر_احرازهویت‌شده_اجرا_می‌شود()
    {
        var provider = BuildProvider(BuildUnauthenticatedUser());
        var behavior = provider.GetRequiredService<IPipelineBehavior<AnonymousCommand, string>>();

        var result = await behavior.HandleAsync(new AnonymousCommand(), () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task command_بدون_کاربر_احرازهویت‌شده_رد_می‌شود()
    {
        var provider = BuildProvider(BuildUnauthenticatedUser());
        var behavior = provider.GetRequiredService<IPipelineBehavior<AuthenticatedOnlyCommand, string>>();

        await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            behavior.HandleAsync(new AuthenticatedOnlyCommand(), () => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task command_با_IRequiresAuthenticatedUser_و_کاربر_واردشده_بدون_permission_خاصی_اجرا_می‌شود()
    {
        var provider = BuildProvider(BuildUser());
        var behavior = provider.GetRequiredService<IPipelineBehavior<AuthenticatedOnlyCommand, string>>();

        var result = await behavior.HandleAsync(new AuthenticatedOnlyCommand(), () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task وقتی_MustChangePassword_true_است_commandهای_معمولی_با_PasswordChangeRequiredException_رد_می‌شوند()
    {
        var provider = BuildProvider(BuildUserRequiringPasswordChange());
        var behavior = provider.GetRequiredService<IPipelineBehavior<AuthenticatedOnlyCommand, string>>();

        await Assert.ThrowsAsync<PasswordChangeRequiredException>(() =>
            behavior.HandleAsync(new AuthenticatedOnlyCommand(), () => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task وقتی_MustChangePassword_true_است_command_با_IAllowedWhenPasswordChangeRequired_همچنان_اجرا_می‌شود()
    {
        var provider = BuildProvider(BuildUserRequiringPasswordChange());
        var behavior = provider.GetRequiredService<IPipelineBehavior<GateExemptCommand, string>>();

        var result = await behavior.HandleAsync(new GateExemptCommand(), () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }
}
