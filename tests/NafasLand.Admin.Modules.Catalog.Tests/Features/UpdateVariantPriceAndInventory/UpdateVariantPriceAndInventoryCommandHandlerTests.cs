using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Modules.Catalog.Features.UpdateVariantPriceAndInventory;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Idempotency;

namespace NafasLand.Admin.Modules.Catalog.Tests.Features.UpdateVariantPriceAndInventory;

public sealed class UpdateVariantPriceAndInventoryCommandHandlerTests
{
    [Fact]
    public async Task تغییر_موفق_PATCH_و_AuditContext_را_با_قبل_و_بعد_پر_می‌کند()
    {
        var portal = new FakePortalProductClient();
        var audit = new CapturingAuditContext();
        var handler = CreateHandler(portal, audit);

        var result = await handler.HandleAsync(
            new UpdateVariantPriceAndInventoryCommand("variant-1", 120_000, 7, 100_000, 4),
            CancellationToken.None);

        Assert.Equal(1, portal.UpdateVariantCallCount);
        Assert.Equal(new PortalVariantPatch(120_000, 7), portal.LastPatch);
        Assert.Equal("variant-1", result.VariantId);
        Assert.Equal("variant-1", audit.EntityId);
        Assert.Equal(100_000m, ReadDecimal(audit.Before, "price"));
        Assert.Equal(4, ReadInt(audit.Before, "stock"));
        Assert.Equal(120_000m, ReadDecimal(audit.After, "price"));
        Assert.Equal(7, ReadInt(audit.After, "stock"));
        Assert.Equal(("Product", "101"), audit.Parent);
    }

    [Theory]
    [InlineData(90_000, 4, "قیمت")]
    [InlineData(100_000, 2, "موجودی")]
    public async Task تغییر_همزمان_قیمت_یا_موجودی_با_409_و_بدون_PATCH_رد_می‌شود(
        decimal lastKnownPrice,
        int lastKnownStock,
        string changedField)
    {
        var portal = new FakePortalProductClient();
        var handler = CreateHandler(portal, new CapturingAuditContext());

        var exception = await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(
            new UpdateVariantPriceAndInventoryCommand("variant-1", 120_000, 7, lastKnownPrice, lastKnownStock),
            CancellationToken.None));

        Assert.Contains(changedField, exception.Message);
        Assert.Equal(0, portal.UpdateVariantCallCount);
    }

    [Fact]
    public async Task واریانت_محصولی_غیر_از_محصول_تستی_با_403_رد_می‌شود()
    {
        var portal = new FakePortalProductClient
        {
            VariantResult = new PortalProductVariant(
                "variant-1", "another-product", "primary", 100_000, null, null, null, null, null, null, null,
                4, null, null, null, null, "commodity", [], []),
        };
        var handler = CreateHandler(portal, new CapturingAuditContext());

        var exception = await Assert.ThrowsAsync<AuthorizationDeniedException>(() => handler.HandleAsync(
            new UpdateVariantPriceAndInventoryCommand("variant-1", 120_000, 7, 100_000, 4),
            CancellationToken.None));

        Assert.Contains("محصول تستی", exception.Message);
        Assert.Equal(0, portal.UpdateVariantCallCount);
    }

    [Fact]
    public void command_متادیتای_permission_idempotency_و_audit_دارد()
    {
        var command = new UpdateVariantPriceAndInventoryCommand("variant-1", 1, 1, 1, 1);

        Assert.Equal(CatalogPermissions.ProductsWrite, command.RequiredPermission);
        Assert.Equal("VariantPriceInventoryUpdated", command.AuditAction);
        Assert.Equal("ProductVariant", command.AuditEntityType);
        Assert.IsAssignableFrom<IIdempotentCommand>(command);
    }

    private static UpdateVariantPriceAndInventoryCommandHandler CreateHandler(
        FakePortalProductClient portal,
        CapturingAuditContext audit)
    {
        return new UpdateVariantPriceAndInventoryCommandHandler(
            portal,
            Options.Create(new PortalOptions { TestProductId = "101" }),
            audit);
    }

    private static decimal ReadDecimal(object? value, string property) =>
        (decimal)value!.GetType().GetProperty(property)!.GetValue(value)!;

    private static int ReadInt(object? value, string property) =>
        (int)value!.GetType().GetProperty(property)!.GetValue(value)!;

    private sealed class CapturingAuditContext : IAuditContext
    {
        public string? EntityId { get; private set; }
        public (string EntityType, string EntityId)? Parent { get; private set; }
        public object? Before { get; private set; }
        public object? After { get; private set; }

        public void SetEntityId(string entityId) => EntityId = entityId;
        public void SetParentEntity(string entityType, string entityId) => Parent = (entityType, entityId);
        public void SetBefore(object? snapshot) => Before = snapshot;
        public void SetAfter(object? snapshot) => After = snapshot;
    }
}
