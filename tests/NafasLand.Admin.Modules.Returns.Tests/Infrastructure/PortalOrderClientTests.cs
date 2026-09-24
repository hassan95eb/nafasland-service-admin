using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Returns.Infrastructure;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;
using NafasLand.Admin.Shared.Infrastructure.Portal;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Modules;

namespace NafasLand.Admin.Modules.Returns.Tests.Infrastructure;

/// <summary>The real HttpClient pipeline (shared limiter, token, resilience) over a fake primary handler — never the real portal (ADR-038).</summary>
public sealed class PortalOrderClientTests
{
    [Fact]
    public async Task سفارش_با_GET_از_مسیر_store_orders_و_با_توکن_خوانده_می‌شود()
    {
        var handler = new RecordingHandler(_ => Respond(HttpStatusCode.OK, OrderFixtures.ReadJson()));
        await using var provider = CreateProvider(handler, includeCatalog: false);

        var order = await provider.GetRequiredService<IPortalOrderClient>().GetOrderAsync(OrderFixtures.OrderId, CancellationToken.None);

        Assert.Equal(OrderFixtures.OrderId, order!.OrderId);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://portal.invalid/site/api/v1/manage/store/orders/900000001", request.Uri);
        Assert.Equal("Bearer test-token-not-a-real-secret", request.Authorization);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "{}")]
    [InlineData(HttpStatusCode.OK, """{"success":false,"description":"raw portal text"}""")]
    public async Task نبود_سفارش_null_برمی‌گرداند(HttpStatusCode status, string body)
    {
        await using var provider = CreateProvider(new RecordingHandler(_ => Respond(status, body)), includeCatalog: false);

        var order = await provider.GetRequiredService<IPortalOrderClient>().GetOrderAsync(1, CancellationToken.None);

        Assert.Null(order);
    }

    [Fact]
    public async Task خطای_پرتال_به_خطای_امن_بدون_پیام_خام_تبدیل_می‌شود()
    {
        var handler = new RecordingHandler(_ => Respond(HttpStatusCode.InternalServerError, "raw sensitive portal message"));
        await using var provider = CreateProvider(handler, includeCatalog: false);

        var exception = await Assert.ThrowsAsync<PortalUnavailableException>(() =>
            provider.GetRequiredService<IPortalOrderClient>().GetOrderAsync(1, CancellationToken.None));

        Assert.DoesNotContain("raw sensitive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Catalog_و_Returns_از_یک_نمونهٔ_محدودکنندهٔ_نرخ_استفاده_می‌کنند()
    {
        var handler = new RecordingHandler(_ => Respond(HttpStatusCode.NotFound, "{}"));
        await using var provider = CreateProvider(handler, includeCatalog: true);

        Assert.Single(provider.GetServices<PortalRateLimiter>());

        // Limit is 2/s with a one-token bucket: whichever call goes second must
        // wait for the other module's permit, which only happens if both
        // clients go through the same limiter instance.
        var stopwatch = Stopwatch.StartNew();
        await Task.WhenAll(
            provider.GetRequiredService<IPortalProductClient>().GetProductAsync("101", CancellationToken.None),
            provider.GetRequiredService<IPortalOrderClient>().GetOrderAsync(1, CancellationToken.None));

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains(handler.Requests, request => request.Uri.Contains("/store/products/101", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, request => request.Uri.Contains("/store/orders/1", StringComparison.Ordinal));
        Assert.True(stopwatch.Elapsed >= TimeSpan.FromMilliseconds(400), $"elapsed {stopwatch.Elapsed}");
    }

    private static ServiceProvider CreateProvider(RecordingHandler handler, bool includeCatalog)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Portal:BaseUrl"] = "https://portal.invalid/site/api/v1/manage",
            ["Portal:BearerToken"] = "test-token-not-a-real-secret",
            ["Portal:TestProductId"] = "101",
            ["Portal:RateLimitPerSecond"] = "2",
            ["Portal:RateLimitQueueCapacity"] = "20",
            ["Portal:RateLimitQueueTimeoutSeconds"] = "5",
            ["Portal:RetryMaxAttempts"] = "1",
            ["Portal:RetryBaseDelaySeconds"] = "0.01",
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICorrelationIdAccessor>(new StubCorrelationIdAccessor());

        new ReturnsModule().RegisterServices(services, configuration);
        if (includeCatalog)
        {
            CreateModule(typeof(IPortalProductClient).Assembly).RegisterServices(services, configuration);
        }

        services.ConfigureAll<HttpClientFactoryOptions>(options =>
            options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = handler));

        return services.BuildServiceProvider();
    }

    private static IModule CreateModule(System.Reflection.Assembly assembly) =>
        (IModule)Activator.CreateInstance(assembly.GetTypes().Single(type => typeof(IModule).IsAssignableFrom(type)))!;

    private static HttpResponseMessage Respond(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body) };

    private sealed class StubCorrelationIdAccessor : ICorrelationIdAccessor
    {
        public string CorrelationId => "test-correlation-id";
    }

    private sealed record RecordedRequest(HttpMethod Method, string Uri, string? Authorization);

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        private readonly ConcurrentQueue<RecordedRequest> _requests = new();

        public IReadOnlyList<RecordedRequest> Requests => _requests.ToList();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requests.Enqueue(new RecordedRequest(request.Method, request.RequestUri!.ToString(), request.Headers.Authorization?.ToString()));
            return Task.FromResult(respond(request));
        }
    }
}
