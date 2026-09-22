using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Modules.Catalog.Features.CreateProduct;
using NafasLand.Admin.Modules.Catalog.Infrastructure;
using NafasLand.Admin.Shared.Infrastructure.Messaging;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Idempotency;

namespace NafasLand.Admin.Modules.Catalog.Tests.Features.CreateProduct;

public sealed class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task محافظ_غیرفعال_بودن_ایجاد_قبل_از_POST_متوقف_می‌کند()
    {
        var portal = new FakePortalProductClient();
        var handler = CreateHandler(portal, allowCreation: false, new CapturingAuditContext());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.HandleAsync(CreateCommand(), CancellationToken.None));

        Assert.Equal(0, portal.CreateProductCallCount);
    }

    [Fact]
    public async Task ایجاد_همیشه_pending_بدون_تصویر_و_با_GET_پس_از_POST_است()
    {
        var portal = new FakePortalProductClient();
        var audit = new CapturingAuditContext();
        var handler = CreateHandler(portal, allowCreation: true, audit);

        var result = await handler.HandleAsync(CreateCommand(), CancellationToken.None);

        Assert.Equal(1, portal.CreateProductCallCount);
        Assert.Equal(1, portal.DetailCallCount);
        Assert.Equal(["pending"], portal.LastCreatedProduct!.Status);
        Assert.Null(portal.LastCreatedProduct.Image);
        Assert.Null(portal.LastCreatedProduct.Images);
        Assert.Null(portal.LastCreatedProduct.Published);
        Assert.Equal("primary", Assert.Single(portal.LastCreatedProduct.Variants).Title);
        Assert.Null(Assert.Single(portal.LastCreatedProduct.Variants).Id);
        Assert.Null(Assert.Single(portal.LastCreatedProduct.Variants).ProductId);
        Assert.DoesNotContain("script", portal.LastCreatedProduct.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("202", result.Id);
        Assert.Equal("202", audit.EntityId);
        Assert.NotNull(audit.After);
    }

    [Fact]
    public void command_از_permission_idempotency_و_audit_استفاده_می‌کند()
    {
        var command = CreateCommand();

        Assert.Equal(CatalogPermissions.ProductsWrite, command.RequiredPermission);
        Assert.IsAssignableFrom<IIdempotentCommand>(command);
        Assert.Equal("ProductCreated", command.AuditAction);
    }

    [Fact]
    public async Task تکرار_همان_فرم_فقط_یک_POST_روی_fake_ایجاد_می‌کند()
    {
        var portal = new FakePortalProductClient();
        var handler = CreateHandler(portal, allowCreation: true, new CapturingAuditContext());
        var store = new InMemoryIdempotencyStore();
        var services = new ServiceCollection().AddSingleton<IIdempotencyStore>(store).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Headers["Idempotency-Key"] = Guid.NewGuid().ToString();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            "Test"));
        var behavior = new IdempotencyBehavior<CreateProductCommand, CreateProductResult>(
            services,
            new HttpContextAccessor { HttpContext = context });
        var command = CreateCommand();

        var first = await behavior.HandleAsync(
            command,
            () => handler.HandleAsync(command, CancellationToken.None),
            CancellationToken.None);
        var second = await behavior.HandleAsync(
            command,
            () => handler.HandleAsync(command, CancellationToken.None),
            CancellationToken.None);

        Assert.Equal(first, second);
        Assert.Equal(1, portal.CreateProductCallCount);
    }

    private static CreateProductCommandHandler CreateHandler(
        FakePortalProductClient portal,
        bool allowCreation,
        CapturingAuditContext audit) => new(
        portal,
        Options.Create(new PortalOptions { TestProductId = "101", AllowProductCreation = allowCreation }),
        new ProductHtmlSanitizer(),
        new ProductCache(TimeProvider.System),
        audit);

    private static CreateProductCommand CreateCommand() => new(
        "محصول تازه",
        null,
        "<p style='font-weight:700'>توضیح<script>bad()</script></p>",
        [new PortalNameValue("معرفی", "<p>متن</p>")],
        true,
        [],
        "محصول-تازه",
        null,
        null,
        "کلیدواژه",
        null,
        null,
        [10],
        [20],
        [],
        [new CreateProductVariantInput("primary", 100, null, null, null, null, null, null, null, 2, 1, 5, "SKU")]);

    private sealed class CapturingAuditContext : IAuditContext
    {
        public string? EntityId { get; private set; }
        public object? After { get; private set; }
        public void SetEntityId(string entityId) => EntityId = entityId;
        public void SetBefore(object? snapshot) { }
        public void SetAfter(object? snapshot) => After = snapshot;
    }

    private sealed class InMemoryIdempotencyStore : IIdempotencyStore
    {
        private Guid? _key;
        private Guid _userId;
        private string? _hash;
        private string? _response;

        public Task<IdempotencyBeginResult> TryBeginAsync(
            Guid key,
            Guid userId,
            string requestHash,
            DateTimeOffset createdAt,
            CancellationToken cancellationToken)
        {
            if (_key is null)
            {
                _key = key;
                _userId = userId;
                _hash = requestHash;
                return Task.FromResult(new IdempotencyBeginResult(IdempotencyBeginOutcome.Started));
            }

            if (_key != key || _userId != userId || _hash != requestHash)
            {
                return Task.FromResult(new IdempotencyBeginResult(IdempotencyBeginOutcome.KeyReused));
            }

            return Task.FromResult(_response is null
                ? new IdempotencyBeginResult(IdempotencyBeginOutcome.InProgress)
                : new IdempotencyBeginResult(IdempotencyBeginOutcome.Completed, _response));
        }

        public Task CompleteAsync(Guid key, string responseJson, CancellationToken cancellationToken)
        {
            _response = responseJson;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid key, CancellationToken cancellationToken)
        {
            _key = null;
            _response = null;
            return Task.CompletedTask;
        }
    }
}
