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

// ---------- Options pattern با اعتبارسنجی در استارتاپ (ADR-039، ADR-012) ----------
// نبود یا نامعتبر بودن هرکدام یعنی برنامه بالا نمی‌آید، نه خطا در اولین درخواست.
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

// ---------- رصدپذیری: Serilog با خروجی JSON و چرخش روزانه (ADR-042) ----------
builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    var loggingOptions = context.Configuration.GetSection(LoggingOptions.SectionName).Get<LoggingOptions>()
        ?? new LoggingOptions();
    var minimumLevel = Enum.Parse<LogEventLevel>(loggingOptions.MinimumLevel);

    // نسبت به AppContext.BaseDirectory حل می‌شود، نه working directory جاری؛
    // وگرنه dotnet watch/dotnet ef/دات‌نت publish هرکدام یک پوشهٔ logs جدا
    // و غیرقابل‌پیش‌بینی می‌سازند.
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

// ---------- احراز هویت ساختگی این گام؛ مدل کامل Identity کار گام ۱ است ----------
builder.Services
    .AddAuthentication(TestUserAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, TestUserAuthenticationHandler>(
        TestUserAuthenticationHandler.SchemeName,
        _ => { });

// ---------- زیرساخت مشترک: CorrelationId، Authorization Policy، pipeline اجباری (ADR-006، ADR-036) ----------
builder.Services.AddSharedInfrastructure();

// ---------- job runner وایر می‌شود، بدون job واقعی (ADR-012) ----------
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

// ---------- ماژول‌ها: کشف خودکار با اسکن اسمبلی (ADR-005)؛ ماژول خاموش رجیستر نمی‌شود (ADR-012) ----------
var moduleDiscovery = builder.Services.AddModules(builder.Configuration);

var app = builder.Build();

// اعتبارسنجی کانفیگ باید همین‌جا و صریح باشد، نه هروقت اولین مصرف‌کننده
// (مثل SampleModule یا Hangfire) به آن نیاز داشت؛ وگرنه کانفیگ نامعتبری که
// هنوز مصرف‌کننده‌اش اجرا نشده، به‌جای پیام روشن OptionsValidationException،
// به یک خطای اتصال گنگ در وسط اجرا می‌رسد (ADR-039).
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

// ---------- بررسی مهاجرت معوق؛ اجرای خودکار مهاجرت در استارتاپ ممنوع است (ADR-041) ----------
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
