using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Api.Authentication;
using NafasLand.Admin.Api.Configuration;
using NafasLand.Admin.Api.HealthChecks;
using NafasLand.Admin.Modules.Auditing.Contracts;
using NafasLand.Admin.Modules.Identity.Contracts;
using NafasLand.Admin.Shared.Infrastructure.Configuration;
using NafasLand.Admin.Shared.Infrastructure.Extensions;
using NafasLand.Admin.Shared.Kernel.Persistence;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

// ---------- Options pattern with startup validation (ADR-039, ADR-012) ----------
// Any of these being missing or invalid means the app does not start, rather than
// erroring on the first request.
builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<PortalOptions>()
    .Bind(builder.Configuration.GetSection(PortalOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<LoggingOptions>()
    .Bind(builder.Configuration.GetSection(LoggingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ---------- Observability: Serilog with JSON output and daily rolling (ADR-042) ----------
builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    var loggingOptions = context.Configuration.GetSection(LoggingOptions.SectionName).Get<LoggingOptions>()
        ?? new LoggingOptions();
    var minimumLevel = Enum.Parse<LogEventLevel>(loggingOptions.MinimumLevel);

    // Resolved against AppContext.BaseDirectory, not the current working
    // directory; otherwise dotnet watch/dotnet ef/dotnet publish would each end
    // up with a separate, unpredictable logs folder.
    var logDirectory = Path.IsPathRooted(loggingOptions.Directory)
        ? loggingOptions.Directory
        : Path.Combine(AppContext.BaseDirectory, loggingOptions.Directory);

    loggerConfiguration
        .MinimumLevel.Is(minimumLevel)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "NafasLand.Admin.Api")
        .WriteTo.Console()
        .WriteTo.File(
            new JsonFormatter(renderMessage: true),
            Path.Combine(logDirectory, "log-.json"),
            rollingInterval: RollingInterval.Day);
});

// Secure is required in production (ADR-013), but the reverse proxy that
// terminates TLS is a later step (ADR-043) — over plain HTTP, ASP.NET Core's
// antiforgery system hard-throws on Cookie.SecurePolicy = Always (not just
// "silently omits the flag", the way cookie auth behaves), which would make
// login impossible for this step's curl-only testing and for local dev.
// SameAsRequest only sets Secure when the actual request was HTTPS.
var cookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

// ---------- Real cookie authentication (ADR-013, ADR-023), replacing step 0's fake user ----------
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "nafasland-admin-session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = cookieSecurePolicy;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        // This is an API, not a page-rendering app: an unauthenticated/forbidden
        // request must get a plain status code, not a redirect to an HTML login page.
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

// Permissions and the forced-password-change flag are recomputed from the
// database on every request (ADR-021), not baked into the cookie at login.
builder.Services.AddScoped<IClaimsTransformation, EffectivePermissionsClaimsTransformation>();

// ---------- Antiforgery for every mutating request except login (ADR-013, ADR-023) ----------
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "nafasland-admin-antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.HeaderName = "X-XSRF-TOKEN";
});

// ---------- Shared infrastructure: CorrelationId, Authorization Policy, mandatory pipeline (ADR-006, ADR-036) ----------
builder.Services.AddSharedInfrastructure();

// ---------- Job runner is wired up, with no real job (ADR-012) ----------
builder.Services.AddHangfire((serviceProvider, config) =>
{
    var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    config.UseSqlServerStorage(databaseOptions.ConnectionString, new SqlServerStorageOptions
    {
        SchemaName = "hangfire",
    });
});
builder.Services.AddHangfireServer();
builder.Services.AddHealthChecks().AddCheck<HangfireHealthCheck>("job-runner");

// ---------- Modules: automatic discovery via assembly scanning (ADR-005); a disabled module is never registered (ADR-012) ----------
var moduleDiscovery = builder.Services.AddModules(builder.Configuration);

var app = builder.Build();

// Config validation must happen right here, explicitly, rather than whenever the
// first consumer (like SampleModule or Hangfire) happens to need it; otherwise
// invalid config whose consumer hasn't run yet surfaces as a confusing
// mid-execution connection error instead of a clear OptionsValidationException
// (ADR-039). Identity's own options (e.g. the SuperAdmin seed credentials) are
// internal to that module (ADR-044) and validated the same way via
// ValidateOnStart, just without an explicit call here — Api has no way to name
// an internal type from another assembly.
_ = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
_ = app.Services.GetRequiredService<IOptions<PortalOptions>>().Value;
_ = app.Services.GetRequiredService<IOptions<LoggingOptions>>().Value;

var enabledModuleNames = string.Join(", ", moduleDiscovery.EnabledModules.Select(m => m.Name).Distinct());
var disabledModuleNames = moduleDiscovery.DisabledModuleNames.Count > 0
    ? string.Join(", ", moduleDiscovery.DisabledModuleNames)
    : "هیچ‌کدام";

Log.Information(
    "ماژول‌های فعال: {EnabledModules}؛ ماژول‌های خاموش: {DisabledModules}",
    enabledModuleNames,
    disabledModuleNames);

// ---------- Check for pending migrations; automatic migration at startup is forbidden (ADR-041) ----------
await using (var scope = app.Services.CreateAsyncScope())
{
    var migrationChecks = scope.ServiceProvider.GetServices<IMigrationCheck>();
    var pending = new List<MigrationCheckResult>();

    foreach (var check in migrationChecks)
    {
        var result = await check.CheckAsync(CancellationToken.None);
        if (result.HasPendingMigrations)
        {
            pending.Add(result);
        }
    }

    if (pending.Count > 0)
    {
        foreach (var result in pending)
        {
            Log.Fatal(
                "مهاجرت معوق برای {ContextName}: {PendingMigrations}",
                result.ContextName,
                string.Join(", ", result.PendingMigrations));
        }

        throw new InvalidOperationException(
            "مهاجرت‌های معوق وجود دارد؛ برنامه بالا نمی‌آید. دستور اجرای مهاجرت در README.md مستند شده است.");
    }
}

// ---------- Identity bootstrap: sync Permission rows from every module, fill SuperAdmin, seed the first SuperAdmin account (ADR-005, ADR-021, ADR-022) ----------
// Runs after the migration check (schema must already exist) and before the app
// starts serving traffic. Module names come from discovery, not from
// PermissionDefinition itself — see ModulePermissionDefinition's own comment.
await using (var scope = app.Services.CreateAsyncScope())
{
    var bootstrapper = scope.ServiceProvider.GetService<IIdentityBootstrapper>();
    if (bootstrapper is not null)
    {
        var allPermissions = moduleDiscovery.EnabledModules
            .SelectMany(discovered => discovered.Module.Permissions
                .Select(definition => new ModulePermissionDefinition(discovered.Name, definition)))
            .ToList();

        await bootstrapper.BootstrapAsync(allPermissions, CancellationToken.None);
    }
}

// ---------- Auditing bootstrap: schedule the 6-month purge recurring job (ADR-015) ----------
// Not done inside AuditingModule.RegisterServices — see IAuditingBootstrapper's
// own comment for why a static Hangfire call there breaks `dotnet ef` tooling.
await using (var scope = app.Services.CreateAsyncScope())
{
    scope.ServiceProvider.GetService<IAuditingBootstrapper>()?.ScheduleRecurringJobs();
}

app.UseExceptionHandler();
app.UseCorrelationId();
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseUserContextLogging();
app.UseAuthorization();
// Not the built-in app.UseAntiforgery() — see AntiforgeryValidationMiddleware's
// own comment for why a plain JSON-body minimal API endpoint isn't covered by it.
app.UseAntiforgeryValidation();

app.MapHealthChecks("/health");
app.MapModuleEndpoints(moduleDiscovery.EnabledModules);

app.Run();
