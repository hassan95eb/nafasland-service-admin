using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Shared.Kernel.Tests.Messaging;

public sealed class ValidationBehaviorTests
{
    private sealed record SampleCommand(string Name) : ICommand<string>;

    private sealed class SampleCommandValidator : AbstractValidator<SampleCommand>
    {
        public SampleCommandValidator()
        {
            RuleFor(c => c.Name).NotEmpty();
        }
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
}
