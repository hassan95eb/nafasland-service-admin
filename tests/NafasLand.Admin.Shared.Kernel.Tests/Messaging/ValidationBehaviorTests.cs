using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Kernel.Tests.Messaging;

public sealed class ValidationBehaviorTests
{
    private sealed record SampleCommand(string Name) : ICommand<string>;

    private sealed record AuditableSampleCommand(string Name) : ICommand<string>, IAuditableCommand
    {
        public string AuditAction => "ProductCreated";
        public string AuditEntityType => "Product";
    }

    private sealed class SampleCommandValidator : AbstractValidator<SampleCommand>
    {
        public SampleCommandValidator()
        {
            RuleFor(c => c.Name).NotEmpty();
        }
    }

    private sealed class AuditableSampleCommandValidator : AbstractValidator<AuditableSampleCommand>
    {
        public AuditableSampleCommandValidator() => RuleFor(command => command.Name).NotEmpty();
    }

    private sealed class FakeAuditLogWriter : IAuditLogWriter
    {
        public List<AuditLogEntry> Entries { get; } = [];
        public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCorrelationIdAccessor : ICorrelationIdAccessor
    {
        public string CorrelationId => "validation-correlation";
    }

    private static ServiceProvider BuildProvider(bool withValidator)
    {
        var services = new ServiceCollection();
        if (withValidator)
        {
            services.AddScoped<IValidator<SampleCommand>, SampleCommandValidator>();
        }

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task بدون_validator_ثبت‌شده_بدون_خطا_رد_می‌شود()
    {
        var provider = BuildProvider(withValidator: false);
        var behavior = provider.GetRequiredService<IPipelineBehavior<SampleCommand, string>>();

        var result = await behavior.HandleAsync(new SampleCommand(""), () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task با_ورودی_نامعتبر_CommandValidationException_با_فهرست_خطا_پرتاب_می‌شود()
    {
        var provider = BuildProvider(withValidator: true);
        var behavior = provider.GetRequiredService<IPipelineBehavior<SampleCommand, string>>();

        var exception = await Assert.ThrowsAsync<CommandValidationException>(() =>
            behavior.HandleAsync(new SampleCommand(""), () => Task.FromResult("ok"), CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey(nameof(SampleCommand.Name)));
    }

    [Fact]
    public async Task با_ورودی_معتبر_next_اجرا_می‌شود()
    {
        var provider = BuildProvider(withValidator: true);
        var behavior = provider.GetRequiredService<IPipelineBehavior<SampleCommand, string>>();

        var result = await behavior.HandleAsync(new SampleCommand("valid"), () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task validation_failure_برای_command_قابل_audit_رکورد_Failed_می‌نویسد()
    {
        var writer = new FakeAuditLogWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<AuditableSampleCommand>, AuditableSampleCommandValidator>();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        services.AddSingleton<IAuditLogWriter>(writer);
        services.AddSingleton<ICorrelationIdAccessor>(new FakeCorrelationIdAccessor());
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        await using var provider = services.BuildServiceProvider();
        var behavior = provider.GetRequiredService<IPipelineBehavior<AuditableSampleCommand, string>>();

        await Assert.ThrowsAsync<CommandValidationException>(() =>
            behavior.HandleAsync(new AuditableSampleCommand(""), () => Task.FromResult("ok"), CancellationToken.None));

        var entry = Assert.Single(writer.Entries);
        Assert.Equal(AuditOutcome.Failed, entry.Outcome);
        Assert.Equal("ValidationFailed", entry.FailureReason);
        Assert.Equal("validation-correlation", entry.CorrelationId);
    }
}
