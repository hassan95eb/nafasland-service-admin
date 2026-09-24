namespace NafasLand.Admin.Modules.Returns.Infrastructure;

/// <summary>
/// The anti-corruption model of a portal order (ADR-008, ADR-054). Only these
/// fields ever leave the portal response; the customer's mobile, address, IP,
/// payments, username and national code are never read (see PortalOrderMapper).
/// Amounts are the portal's raw numbers — no unit conversion (rule 13).
/// </summary>
internal sealed record PortalOrder(
    long OrderId,
    IReadOnlyList<string> Statuses,
    string? CustomerName,
    decimal? Subtotal,
    decimal? Shipping,
    decimal? Discount,
    decimal? Tax,
    decimal? Total,
    DateTime? CreatedAtUtc,
    IReadOnlyList<PortalOrderItem> Items);

internal sealed record PortalOrderItem(
    long? ProductId,
    long? VariantId,
    string? Title,
    decimal? Price,
    int Quantity);
