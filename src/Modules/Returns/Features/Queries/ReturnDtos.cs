using NafasLand.Admin.Modules.Returns.Infrastructure;
using NafasLand.Admin.Modules.Returns.Persistence;
using NafasLand.Admin.Shared.Kernel.Users;

namespace NafasLand.Admin.Modules.Returns.Features.Queries;

internal sealed record OrderItemDto(long? ProductId, long? VariantId, string? Title, decimal? Price, int Quantity);

/// <summary>
/// The order as the return form shows it — only the fields of ADR-054's
/// mapping table. IneligibilityReason is set when a return cannot be filed
/// for this order (unpaid, canceled, or already returned), so the form can
/// show the order and still explain why it is disabled.
/// </summary>
internal sealed record OrderPreviewDto(
    long OrderId,
    IReadOnlyList<string> Statuses,
    string? CustomerName,
    decimal? Subtotal,
    decimal? Shipping,
    decimal? Discount,
    decimal? Tax,
    decimal? Total,
    DateTime? CreatedAtUtc,
    IReadOnlyList<OrderItemDto> Items,
    string? IneligibilityReason)
{
    public static OrderPreviewDto FromOrder(PortalOrder order, string? ineligibilityReason) => new(
        order.OrderId,
        order.Statuses,
        order.CustomerName,
        order.Subtotal,
        order.Shipping,
        order.Discount,
        order.Tax,
        order.Total,
        order.CreatedAtUtc,
        order.Items.Select(item => new OrderItemDto(item.ProductId, item.VariantId, item.Title, item.Price, item.Quantity)).ToList(),
        ineligibilityReason);
}

/// <summary>Usernames are null for an unknown or removed user; the UI labels that «کاربر ناشناس».</summary>
internal sealed record ReturnRecordSummaryDto(
    Guid Id,
    long OrderId,
    string? CustomerName,
    decimal? Total,
    DateOnly ReturnDate,
    Guid RegisteredByUserId,
    string? RegisteredByUsername,
    DateTime RegisteredAt,
    Guid ApprovedByUserId,
    string? ApprovedByUsername,
    DateTime ApprovedAt)
{
    public static ReturnRecordSummaryDto FromEntity(ReturnRecord record, IReadOnlyDictionary<Guid, string> usernames) => new(
        record.Id,
        record.OrderId,
        record.CustomerName,
        record.Total,
        record.ReturnDate,
        record.RegisteredByUserId,
        usernames.GetValueOrDefault(record.RegisteredByUserId),
        record.RegisteredAt,
        record.ApprovedByUserId,
        usernames.GetValueOrDefault(record.ApprovedByUserId),
        record.ApprovedAt);
}

internal sealed record ReturnRecordPageDto(IReadOnlyList<ReturnRecordSummaryDto> Items, string? NextCursor)
{
    public static async Task<ReturnRecordPageDto> CreateAsync(
        IReadOnlyList<ReturnRecord> rowsPlusOne,
        int pageSize,
        IUserDirectory userDirectory,
        CancellationToken cancellationToken)
    {
        var hasMore = rowsPlusOne.Count > pageSize;
        var page = hasMore ? rowsPlusOne.Take(pageSize).ToList() : rowsPlusOne;
        var nextCursor = hasMore ? ReturnRecordQueryExtensions.EncodeCursor(page[^1]) : null;

        var userIds = page.SelectMany(record => new[] { record.RegisteredByUserId, record.ApprovedByUserId }).Distinct().ToList();
        var usernames = await userDirectory.GetUsernamesAsync(userIds, cancellationToken);

        return new ReturnRecordPageDto(page.Select(record => ReturnRecordSummaryDto.FromEntity(record, usernames)).ToList(), nextCursor);
    }
}

internal sealed record ReturnRecordDetailDto(
    Guid Id,
    long OrderId,
    string? CustomerName,
    IReadOnlyList<string> OrderStatuses,
    DateTime? OrderCreatedAtUtc,
    decimal? Subtotal,
    decimal? Shipping,
    decimal? Discount,
    decimal? Tax,
    decimal? Total,
    IReadOnlyList<OrderItemDto> Items,
    string Reason,
    DateOnly ReturnDate,
    Guid RegisteredByUserId,
    string? RegisteredByUsername,
    DateTime RegisteredAt,
    Guid ApprovedByUserId,
    string? ApprovedByUsername,
    DateTime ApprovedAt)
{
    public static ReturnRecordDetailDto FromEntity(ReturnRecord record, IReadOnlyDictionary<Guid, string> usernames) => new(
        record.Id,
        record.OrderId,
        record.CustomerName,
        record.OrderStatuses,
        record.OrderCreatedAtUtc,
        record.Subtotal,
        record.Shipping,
        record.Discount,
        record.Tax,
        record.Total,
        record.Items.Select(item => new OrderItemDto(item.ProductId, item.VariantId, item.Title, item.Price, item.Quantity)).ToList(),
        record.Reason,
        record.ReturnDate,
        record.RegisteredByUserId,
        usernames.GetValueOrDefault(record.RegisteredByUserId),
        record.RegisteredAt,
        record.ApprovedByUserId,
        usernames.GetValueOrDefault(record.ApprovedByUserId),
        record.ApprovedAt);
}
