using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Catalog.Tests.Infrastructure;

public sealed class PortalResilienceTests
{
    [Fact]
    public async Task بدنه_POST_محصول_از_قرارداد_snake_case_و_value_status_استفاده_می‌کند()
    {
        var handler = new CapturingRequestHandler();
        await using var provider = CreateProvider(handler, retryAttempts: 1);
        var client = provider.GetRequiredService<IPortalProductClient>();
        var variant = new PortalProductVariant(
            null, null, "primary", 100, null, null, null, null, null, null, null,
            2, null, null, null, null, "commodity", ["available"], []);
        var write = new PortalProductWriteModel(
            "عنوان", null, null, [], false, [], null, null, null, "کلید", null, null,
            null, null, null, [1], [2], [], [new PortalAttribute("رنگ", ["قرمز"])],
            [variant], ["pending"], null);

        var result = await client.CreateProductAsync(write, CancellationToken.None);

        Assert.Equal("202", result.Id);
        using var body = JsonDocument.Parse(handler.LastBody);
        var root = body.RootElement;
        Assert.False(root.GetProperty("commenting_enabled").GetBoolean());
        var attribute = Assert.Single(root.GetProperty("attributes").EnumerateArray());
        Assert.Equal("رنگ", attribute.GetProperty("name").GetString());
        Assert.Equal("قرمز", Assert.Single(attribute.GetProperty("value").EnumerateArray()).GetString());
        var writtenVariant = Assert.Single(root.GetProperty("variants").EnumerateArray());
        Assert.Equal("available", Assert.Single(writtenVariant.GetProperty("status").EnumerateArray()).GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("image").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("images").ValueKind);
    }

    [Fact]
    public async Task پاسخ_429_با_رعایت_RetryAfter_دوباره_تلاش_می‌شود()
    {
        var handler = new SequenceHandler(attempt => attempt == 1
            ? CreateResponse(HttpStatusCode.TooManyRequests, retryAfterSeconds: 1)
            : CreateResponse(HttpStatusCode.OK, ReadFixture("products-list.json")));
        await using var provider = CreateProvider(handler, retryAttempts: 2);
        var client = provider.GetRequiredService<IPortalProductClient>();
        var stopwatch = Stopwatch.StartNew();

        var result = await client.ListProductsAsync(
            new PortalProductListQuery(1, 25, null, null),
            CancellationToken.None);

        Assert.Equal(2, handler.CallCount);
        Assert.True(stopwatch.Elapsed >= TimeSpan.FromMilliseconds(800));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task POST_محصول_در_لایه_HTTP_هرگز_retry_نمی‌شود()
    {
        var handler = new SequenceHandler(_ => CreateResponse(HttpStatusCode.ServiceUnavailable));
        await using var provider = CreateProvider(handler, retryAttempts: 3);
        var client = provider.GetRequiredService<IPortalProductClient>();
        var write = new PortalProductWriteModel(
            "عنوان", null, null, [], false, [], null, null, null, null, null, null,
            null, null, null, [], [], [], [], [], ["pending"], null);

        await Assert.ThrowsAsync<PortalUnavailableException>(() =>
            client.CreateProductAsync(write, CancellationToken.None));

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task circuit_breaker_بعد_از_شکست‌ها_بدون_رسیدن_به_handler_رد_می‌کند()
    {
        var handler = new SequenceHandler(_ => CreateResponse(HttpStatusCode.ServiceUnavailable));
        await using var provider = CreateProvider(handler, retryAttempts: 1, minimumThroughput: 2);
        var client = provider.GetRequiredService<IPortalProductClient>();

        await Assert.ThrowsAsync<PortalUnavailableException>(() => client.ListProductsAsync(
            new PortalProductListQuery(1, 25, null, null),
            CancellationToken.None));
        var attemptsBeforeOpenCall = handler.CallCount;

        await Assert.ThrowsAsync<PortalUnavailableException>(() => client.ListProductsAsync(
            new PortalProductListQuery(1, 25, null, null),
            CancellationToken.None));

        Assert.Equal(2, attemptsBeforeOpenCall);
        Assert.Equal(attemptsBeforeOpenCall, handler.CallCount);
    }

    [Fact]
    public async Task timeout_هر_تلاش_را_متوقف_می‌کند()
    {
        var handler = new SequenceHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return CreateResponse(HttpStatusCode.OK, ReadFixture("products-list.json"));
        });
        await using var provider = CreateProvider(handler, retryAttempts: 1, timeoutSeconds: 0.1);
        var client = provider.GetRequiredService<IPortalProductClient>();

        await Assert.ThrowsAsync<PortalUnavailableException>(() => client.ListProductsAsync(
            new PortalProductListQuery(1, 25, null, null),
            CancellationToken.None));

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task success_false_به_خطای_امن_تبدیل_و_پیام_خام_پرتال_پنهان_می‌شود()
    {
        var handler = new SequenceHandler(_ => CreateResponse(
            HttpStatusCode.OK,
            "{\"success\":false,\"description\":\"raw sensitive portal message\"}"));
        await using var provider = CreateProvider(handler, retryAttempts: 1);
        var client = provider.GetRequiredService<IPortalProductClient>();

        var exception = await Assert.ThrowsAsync<PortalUnavailableException>(() => client.ListProductsAsync(
            new PortalProductListQuery(1, 25, null, null),
            CancellationToken.None));

        Assert.DoesNotContain("raw sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceProvider CreateProvider(
        HttpMessageHandler handler,
        int retryAttempts,
        int minimumThroughput = 4,
        double timeoutSeconds = 1)
    {
        var values = new Dictionary<string, string?>
        {
            ["Portal:BaseUrl"] = "https://portal.invalid/site/api/v1/manage",
            ["Portal:BearerToken"] = "test-token-not-a-real-secret",
            ["Portal:TestProductId"] = "101",
            ["Portal:RateLimitPerSecond"] = "2",
            ["Portal:RateLimitQueueCapacity"] = "20",
            ["Portal:RateLimitQueueTimeoutSeconds"] = "3",
            ["Portal:RetryMaxAttempts"] = retryAttempts.ToString(),
            ["Portal:RetryBaseDelaySeconds"] = "0.01",
            ["Portal:CircuitBreakerMinimumThroughput"] = minimumThroughput.ToString(),
            ["Portal:CircuitBreakerSamplingDurationSeconds"] = "2",
            ["Portal:CircuitBreakerBreakDurationSeconds"] = "5",
            ["Portal:AttemptTimeoutSeconds"] = timeoutSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<PortalOptions>().Bind(configuration.GetSection(PortalOptions.SectionName));
        services.AddSingleton<ICorrelationIdAccessor>(new StubCorrelationIdAccessor());

        new CatalogModule().RegisterServices(services, configuration);
        services.AddHttpClient<IPortalProductClient, PortalProductClient>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider();
    }

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        string content = "{}",
        int? retryAfterSeconds = null)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content),
        };

        if (retryAfterSeconds is not null)
        {
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
                TimeSpan.FromSeconds(retryAfterSeconds.Value));
        }

        return response;
    }

    private static string ReadFixture(string name)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
    }

    private sealed class StubCorrelationIdAccessor : ICorrelationIdAccessor
    {
        public string CorrelationId => "test-correlation-id";
    }

    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Func<int, CancellationToken, Task<HttpResponseMessage>> _responseFactory;
        private int _callCount;

        public SequenceHandler(Func<int, HttpResponseMessage> responseFactory)
            : this((attempt, _) => Task.FromResult(responseFactory(attempt)))
        {
        }

        public SequenceHandler(Func<int, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public int CallCount => Volatile.Read(ref _callCount);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref _callCount);
            return _responseFactory(attempt, cancellationToken);
        }
    }

    private sealed class CapturingRequestHandler : HttpMessageHandler
    {
        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return CreateResponse(HttpStatusCode.OK, "{\"success\":true,\"id\":202}");
        }
    }
}
