using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Returns.Infrastructure;
using NafasLand.Admin.Modules.Returns.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Infrastructure.Extensions;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Modules;
using NafasLand.Admin.Shared.Kernel.Users;

namespace NafasLand.Admin.Modules.Returns.Tests;

/// <summary>
/// Approvals and Returns on a real Kestrel host with the real mandatory
/// pipeline, so filing goes through the actual POST /api/v1/approvals and a
/// refused filing really reaches AuditLog as Denied. Both modules are loaded
/// the way the host loads them (reflection over IModule — Returns never
/// references Approvals), their DbContexts are swapped to InMemory and the
/// portal is a fake (ADR-038). Callers pick the user with X-Test-User and
/// grant permissions with a comma-separated X-Test-Permission header.
/// </summary>
internal sealed class ReturnsEndpointHost(WebApplication app, HttpClient client, RecordingAuditLogWriter auditLog) : IAsyncDisposable
{
    public const string UserHeader = "X-Test-User";
    public const string PermissionHeader = "X-Test-Permission";

    private static readonly Assembly ApprovalsAssembly = Assembly.Load("NafasLand.Admin.Modules.Approvals");

    public HttpClient Client { get; } = client;

    public RecordingAuditLogWriter AuditLog { get; } = auditLog;

    public static async Task<ReturnsEndpointHost> StartAsync(
        FakePortalOrderClient portal,
        IReadOnlyDictionary<Guid, string>? usernames = null)
    {
        var databaseSuffix = Guid.NewGuid().ToString();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Logging.ClearProviders();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Portal:BaseUrl"] = "https://portal.invalid/site/api/v1/manage",
            ["Portal:BearerToken"] = "test-token-not-a-real-secret",
        }).Build();

        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        builder.Services.AddSharedInfrastructure();
        builder.Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new FixedTimeProvider(OrderFixtures.Now)));

        var auditLog = new RecordingAuditLogWriter();
        builder.Services.AddSingleton<IAuditLogWriter>(auditLog);
        builder.Services.AddSingleton<IUserDirectory>(new FakeUserDirectory(usernames));

        var modules = new[] { CreateModule(ApprovalsAssembly), new ReturnsModule() };
        foreach (var module in modules)
        {
            module.RegisterServices(builder.Services, configuration);
        }

        UseInMemory(builder.Services, ApprovalsAssembly.GetType("NafasLand.Admin.Modules.Approvals.Persistence.ApprovalsDbContext", throwOnError: true)!, $"approvals-{databaseSuffix}");
        UseInMemory(builder.Services, typeof(ReturnsDbContext), $"returns-{databaseSuffix}");
        builder.Services.AddScoped<IPortalOrderClient>(_ => portal);

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        foreach (var module in modules)
        {
            module.MapEndpoints(app);
        }

        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new ReturnsEndpointHost(app, new HttpClient { BaseAddress = new Uri(address) }, auditLog);
    }

    public async Task<List<ReturnRecord>> ReadRecordsAsync()
    {
        await using var scope = app.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ReturnsDbContext>().ReturnRecords.AsNoTracking().ToListAsync();
    }

    public void ActAs(Guid userId, params string[] permissions)
    {
        Client.DefaultRequestHeaders.Remove(UserHeader);
        Client.DefaultRequestHeaders.Remove(PermissionHeader);
        Client.DefaultRequestHeaders.Add(UserHeader, userId.ToString());
        if (permissions.Length > 0)
        {
            Client.DefaultRequestHeaders.Add(PermissionHeader, string.Join(',', permissions));
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();
    }

    private static IModule CreateModule(Assembly assembly) =>
        (IModule)Activator.CreateInstance(assembly.GetTypes().Single(type => typeof(IModule).IsAssignableFrom(type)))!;

    /// <summary>Replaces a module's SQL Server registration with InMemory (the module's own AddDbContext is otherwise kept).</summary>
    private static void UseInMemory(IServiceCollection services, Type contextType, string databaseName)
    {
        services.RemoveAll(typeof(IDbContextOptionsConfiguration<>).MakeGenericType(contextType));
        typeof(ReturnsEndpointHost)
            .GetMethod(nameof(ConfigureInMemory), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(contextType)
            .Invoke(null, [services, databaseName]);
    }

    private static void ConfigureInMemory<TContext>(IServiceCollection services, string databaseName)
        where TContext : DbContext =>
        services.ConfigureDbContext<TContext>(options => options
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var userId = Request.Headers[UserHeader].ToString();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, string.IsNullOrEmpty(userId) ? Guid.NewGuid().ToString() : userId),
                new(ClaimTypes.Role, "Admin"),
            };

            var permissions = Request.Headers[PermissionHeader].ToString();
            claims.AddRange(permissions
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(permission => new Claim(PermissionClaimTypes.Permission, permission)));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
