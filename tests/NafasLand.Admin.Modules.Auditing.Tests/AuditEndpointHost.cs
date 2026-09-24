using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Auditing.Configuration;
using NafasLand.Admin.Modules.Auditing.Features.ExportAuditLog;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Infrastructure.Extensions;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Messaging;
using NafasLand.Admin.Shared.Kernel.Users;

namespace NafasLand.Admin.Modules.Auditing.Tests;

/// <summary>
/// The Auditing endpoints on a real Kestrel host with the real mandatory
/// pipeline (so export's AuditExported/Denied rows are really written) over an
/// InMemory AuditingDbContext. Callers pick the user with X-Test-User and grant
/// permissions with a comma-separated X-Test-Permission header.
/// </summary>
internal sealed class AuditEndpointHost(WebApplication app, HttpClient client) : IAsyncDisposable
{
    public const string UserHeader = "X-Test-User";
    public const string PermissionHeader = "X-Test-Permission";

    public HttpClient Client { get; } = client;

    public IServiceProvider Services => app.Services;

    public static async Task<AuditEndpointHost> StartAsync(IReadOnlyDictionary<Guid, string>? usernames = null)
    {
        var databaseName = Guid.NewGuid().ToString();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Logging.ClearProviders();

        builder.Services
            .AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        builder.Services.AddSharedInfrastructure();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddDbContext<AuditingDbContext>(options => options.UseInMemoryDatabase(databaseName));
        builder.Services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        builder.Services.AddSingleton<IUserDirectory>(new FakeUserDirectory(usernames));
        builder.Services.AddSingleton<IBackgroundJobClient>(new FakeBackgroundJobClient());
        builder.Services.AddSingleton(Options.Create(new AuditExportOptions()));
        builder.Services.AddScoped<IValidator<ExportAuditLogCommand>, ExportAuditLogCommandValidator>();
        builder.Services.AddScoped<ICommandHandler<ExportAuditLogCommand, ExportAuditLogResult>, ExportAuditLogCommandHandler>();

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        new AuditingModule().MapEndpoints(app);
        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new AuditEndpointHost(app, new HttpClient { BaseAddress = new Uri(address) });
    }

    public async Task SeedAsync(params AuditLog[] logs)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditingDbContext>();
        dbContext.AuditLogs.AddRange(logs);
        await dbContext.SaveChangesAsync();
    }

    public async Task SeedProductRefAsync(string externalProductId, string title)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditingDbContext>();
        dbContext.ProductRefs.Add(ProductRef.Create(externalProductId, title, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> ReadLogsAsync()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditingDbContext>();
        return await dbContext.AuditLogs.AsNoTracking().ToListAsync();
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

    public static AuditLog Log(
        string action,
        DateTime createdAt,
        Guid? actorUserId = null,
        string? entityType = null,
        string? entityId = null,
        AuditOutcome outcome = AuditOutcome.Success,
        string? beforeJson = null,
        string? afterJson = null,
        string? parentEntityType = null,
        string? parentEntityId = null) =>
        AuditLog.FromEntry(
            new AuditLogEntry(
                Guid.NewGuid().ToString(), actorUserId, null, actorUserId is null ? "System" : "Admin", action,
                entityType, entityId, beforeJson, afterJson, null, outcome, null, null, null, null,
                parentEntityType, parentEntityId),
            createdAt);

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
                new(ClaimTypes.Role, "SuperAdmin"),
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
