using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Api.Configuration;
using NafasLand.Admin.Api.HealthChecks;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
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

// ---------- This step's fake authentication; the full Identity model is step 1's job ----------
builder.Services
    .AddAuthentication(TestUserAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, TestUserAuthenticationHandler>(
        TestUserAuthenticationHandler.SchemeName,
        _ => { });

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
// (ADR-039).
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

app.UseExceptionHandler();
app.UseCorrelationId();
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseUserContextLogging();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapModuleEndpoints(moduleDiscovery.EnabledModules);

app.Run();
