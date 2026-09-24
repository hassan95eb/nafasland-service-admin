using Microsoft.Extensions.Options;
using NafasLand.Admin.Modules.Catalog.Contracts;
using NafasLand.Admin.Modules.Catalog.Contracts.Configuration;
using NafasLand.Admin.Modules.Catalog.Contracts.Models;
using NafasLand.Admin.Shared.Kernel.Auditing;
using NafasLand.Admin.Shared.Kernel.Errors;
using NafasLand.Admin.Shared.Kernel.Messaging;

namespace NafasLand.Admin.Modules.Catalog.Features.UpdateVariantPriceAndInventory;

internal sealed class UpdateVariantPriceAndInventoryCommandHandler(
    IPortalProductClient portalClient,
    IOptions<PortalOptions> portalOptions,
    IAuditContext auditContext)
    : ICommandHandler<UpdateVariantPriceAndInventoryCommand, UpdateVariantPriceAndInventoryResult>
{
    public async Task<UpdateVariantPriceAndInventoryResult> HandleAsync(
        UpdateVariantPriceAndInventoryCommand command,
        CancellationToken cancellationToken)
    {
        var current = await portalClient.GetVariantAsync(command.VariantId, cancellationToken);
        if (current is null)
        {
            throw ValidationError("VariantId", "واریانت در نفس‌لند پیدا نشد.");
        }

        if (!portalOptions.Value.IsWriteAllowed(current.ProductId))
        {
            throw new AuthorizationDeniedException(
                "محافظ محیط توسعه فقط اجازهٔ نوشتن روی محصول تستی تعیین‌شده را می‌دهد.");
        }

        var conflicts = new List<string>();
        if (current.Price != command.LastKnownPrice)
        {
            conflicts.Add($"قیمت از زمان بازکردن فرم توسط کاربر دیگری تغییر کرده: {Format(current: command.LastKnownPrice)} → {Format(current.Price)}");
        }

        if (current.Stock != command.LastKnownStock)
        {
            conflicts.Add($"موجودی از زمان بازکردن فرم توسط کاربر دیگری تغییر کرده: {Format(command.LastKnownStock)} → {Format(current.Stock)}");
        }

        if (conflicts.Count > 0)
        {
            throw new ConflictException(string.Join(" ", conflicts));
        }

        auditContext.SetEntityId(command.VariantId);
        if (!string.IsNullOrEmpty(current.ProductId))
        {
            // Lets the per-product history (ADR-014, view 4) find this variant change.
            auditContext.SetParentEntity("Product", current.ProductId);
        }
        auditContext.SetBefore(new { price = current.Price, stock = current.Stock });

        await portalClient.UpdateVariantAsync(
            command.VariantId,
            new PortalVariantPatch(command.NewPrice, command.NewStock),
            cancellationToken);

        auditContext.SetAfter(new { price = command.NewPrice, stock = command.NewStock });
        return new UpdateVariantPriceAndInventoryResult(command.VariantId, command.NewPrice, command.NewStock);
    }

    private static string Format(object? current) => current?.ToString() ?? "تعریف‌نشده";

    private static CommandValidationException ValidationError(string field, string message)
    {
        return new CommandValidationException(new Dictionary<string, string[]>
        {
            [field] = [message],
        });
    }
}
