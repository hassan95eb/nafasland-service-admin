using System.Text.Json;
using NafasLand.Admin.Modules.Returns.Infrastructure;

namespace NafasLand.Admin.Modules.Returns.Persistence;

/// <summary>
/// A return, created only after a SuperAdmin approves it (ADR-054). The order
/// is stored as a snapshot taken at approval time, so a later change to the
/// order on the portal never rewrites this history. The only personal data
/// kept is the customer's name.
///
/// Items and statuses are stored as JSON columns rather than a child table:
/// they are a frozen snapshot that is always read and written together with
/// the record and never queried on their own.
/// </summary>
internal sealed class ReturnRecord
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private ReturnRecord()
    {
        OrderStatusesJson = "[]";
        ItemsJson = "[]";
        Reason = string.Empty;
    }

    public Guid Id { get; private set; }

    /// <summary>Unique: a retry after ExecutionFailed can never add a second row (rule 18).</summary>
    public Guid ApprovalRequestId { get; private set; }

    /// <summary>Unique: one return per order (ADR-054).</summary>
    public long OrderId { get; private set; }

    public string? CustomerName { get; private set; }

    public decimal? Subtotal { get; private set; }

    public decimal? Shipping { get; private set; }

    public decimal? Discount { get; private set; }

    public decimal? Tax { get; private set; }

    public decimal? Total { get; private set; }

    public DateTime? OrderCreatedAtUtc { get; private set; }

    public string OrderStatusesJson { get; private set; }

    public string ItemsJson { get; private set; }

    public string Reason { get; private set; }

    /// <summary>A calendar day in Tehran (entered as a Jalali date in the UI), not an instant — see ADR-035.</summary>
    public DateOnly ReturnDate { get; private set; }

    public Guid RegisteredByUserId { get; private set; }

    public DateTime RegisteredAt { get; private set; }

    public Guid ApprovedByUserId { get; private set; }

    public DateTime ApprovedAt { get; private set; }

    public IReadOnlyList<string> OrderStatuses =>
        JsonSerializer.Deserialize<List<string>>(OrderStatusesJson, JsonOptions) ?? [];

    public IReadOnlyList<ReturnRecordItem> Items =>
        JsonSerializer.Deserialize<List<ReturnRecordItem>>(ItemsJson, JsonOptions) ?? [];

    public static ReturnRecord Create(
        Guid approvalRequestId,
        PortalOrder order,
        string reason,
        DateOnly returnDate,
        Guid registeredByUserId,
        DateTime registeredAtUtc,
        Guid approvedByUserId,
        DateTime approvedAtUtc)
    {
        var items = order.Items
            .Select(item => new ReturnRecordItem(item.ProductId, item.VariantId, item.Title, item.Price, item.Quantity))
            .ToList();

        return new ReturnRecord
        {
            Id = Guid.NewGuid(),
            ApprovalRequestId = approvalRequestId,
            OrderId = order.OrderId,
            CustomerName = order.CustomerName,
            Subtotal = order.Subtotal,
            Shipping = order.Shipping,
            Discount = order.Discount,
            Tax = order.Tax,
            Total = order.Total,
            OrderCreatedAtUtc = order.CreatedAtUtc,
            OrderStatusesJson = JsonSerializer.Serialize(order.Statuses, JsonOptions),
            ItemsJson = JsonSerializer.Serialize(items, JsonOptions),
            Reason = reason,
            ReturnDate = returnDate,
            RegisteredByUserId = registeredByUserId,
            RegisteredAt = registeredAtUtc,
            ApprovedByUserId = approvedByUserId,
            ApprovedAt = approvedAtUtc,
        };
    }
}

internal sealed record ReturnRecordItem(long? ProductId, long? VariantId, string? Title, decimal? Price, int Quantity);
