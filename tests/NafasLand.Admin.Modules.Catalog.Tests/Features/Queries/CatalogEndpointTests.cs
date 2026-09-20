using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Features.Queries;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Infrastructure.Authorization;
using NafasLand.Admin.Shared.Infrastructure.CorrelationId;

namespace NafasLand.Admin.Modules.Catalog.Tests.Features.Queries;

public sealed class CatalogEndpointTests
{
    [Fact]
    public async Task هر_دو_endpoint_بدون_permission_کد_403_می‌دهند()
    {
        var fake = new FakePortalProductClient();
        await using var host = await EndpointHost.StartAsync(fake);

        var listResponse = await host.Client.GetAsync("/api/v1/catalog/products?page=1&pageSize=25");
        var detailResponse = await host.Client.GetAsync("/api/v1/catalog/products/101");

        Assert.Equal(HttpStatusCode.Forbidden, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, detailResponse.StatusCode);
        Assert.Equal(0, fake.ListCallCount);
        Assert.Equal(0, fake.DetailCallCount);
    }

    [Fact]
    public async Task فراخوانی_دوم_فهرست_از_کش_می‌آید()
    {
        var fake = new FakePortalProductClient();
        await using var host = await EndpointHost.StartAsync(fake);
        host.Client.DefaultRequestHeaders.Add("X-Test-Permission", CatalogPermissions.ProductsRead);

        var first = await host.Client.GetAsync("/api/v1/catalog/products?page=1&pageSize=25&keywords=پوست&sorting=newest");
        var second = await host.Client.GetAsync("/api/v1/catalog/products?page=1&pageSize=25&keywords=پوست&sorting=newest");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(1, fake.ListCallCount);
    }

    [Fact]
    public async Task جزئیات_هیچ‌گاه_کش_نمی‌شود()
    {
        var fake = new FakePortalProductClient();
        await using var host = await EndpointHost.StartAsync(fake);
        host.Client.DefaultRequestHeaders.Add("X-Test-Permission", CatalogPermissions.ProductsRead);

        var first = await host.Client.GetAsync("/api/v1/catalog/products/101");
        var second = await host.Client.GetAsync("/api/v1/catalog/products/101");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(2, fake.DetailCallCount);
    }

    [Fact]
    public async Task محصول_ناموجود_به_ProblemDetails_404_تبدیل_می‌شود()
    {
        var fake = new FakePortalProductClient { DetailResult = null };
        await using var host = await EndpointHost.StartAsync(fake);
        host.Client.DefaultRequestHeaders.Add("X-Test-Permission", CatalogPermissions.ProductsRead);

        var response = await host.Client.GetAsync("/api/v1/catalog/products/missing");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("test-correlation-id", body);
    }

    private sealed class EndpointHost(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public static async Task<EndpointHost> StartAsync(FakePortalProductClient fake)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
            builder.Services
                .AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
            builder.Services.AddAuthorization();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton(fake);
            builder.Services.AddSingleton<IPortalProductClient>(fake);
            builder.Services.AddSingleton<ProductListCache>();
            builder.Services.AddSingleton<ICorrelationIdAccessor>(new StubCorrelationIdAccessor());

            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            ListProductsEndpoint.Map(app);
            GetProductEndpoint.Map(app);
            await app.StartAsync();

            var server = app.Services.GetRequiredService<IServer>();
            var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            return new EndpointHost(app, new HttpClient { BaseAddress = new Uri(address) });
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
            var permission = Request.Headers["X-Test-Permission"].ToString();
            if (!string.IsNullOrWhiteSpace(permission))
            {
                claims.Add(new Claim(PermissionClaimTypes.Permission, permission));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }

    private sealed class StubCorrelationIdAccessor : ICorrelationIdAccessor
    {
        public string CorrelationId => "test-correlation-id";
    }
}
