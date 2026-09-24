using System.Globalization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NafasLand.Admin.Modules.Returns.Infrastructure;
using NafasLand.Admin.Modules.Returns.Persistence;
using NafasLand.Admin.Shared.Kernel.Approvals;
using NafasLand.Admin.Shared.Kernel.Errors;

namespace NafasLand.Admin.Modules.Returns.Approvals;

/// <summary>
/// returns.register (ADR-054). Filed by an Admin through the existing
/// POST /api/v1/approvals, decided by a SuperAdmin. Nothing is written to the
/// portal: approval only stores a ReturnRecord with a snapshot of the order,
/// read live from the portal at that moment, in this module's own schema.
///
/// PreviewAsync runs when the request is filed (and again when a reviewer
/// opens it), so its exceptions are what stop an invalid filing: 400 for a
/// bad payload, 422 for an order that cannot be returned, 409 for an order
/// that already has a return.
/// </summary>
internal sealed class RegisterReturnApprovalExecutor(
    IPortalOrderClient portalClient,
    ReturnsDbContext dbContext,
    IValidator<RegisterReturnPayload> payloadValidator) : IApprovalExecutor
{
    public const string RequestTypeKey = "returns.register";

    public string RequestType => RequestTypeKey;

    public string RequestPermission => ReturnsPermissions.Request;

    public string RequiredPermission => ReturnsPermissions.Review;

    public async Task<ApprovalPreview> PreviewAsync(string payloadJson, CancellationToken cancellationToken)
    {
        var payload = RegisterReturnPayload.Parse(payloadJson, payloadValidator);

        // Approvals files the request anyway when a preview throws
        // ResourceNotFoundException (the preview is "best effort" there), so a
        // missing order is reported as a business rule to actually stop it.
        var order = await portalClient.GetOrderAsync(payload.OrderId, cancellationToken)
            ?? throw new BusinessRuleException(ReturnRules.OrderNotFound);

        var ruleFailure = ReturnRules.CheckOrderEligibility(order)
            ?? ReturnRules.CheckReturnDate(payload.ReturnDate!.Value, order);
        if (ruleFailure is not null)
        {
            throw new BusinessRuleException(ruleFailure);
        }

        if (await dbContext.ReturnRecords.AnyAsync(record => record.OrderId == order.OrderId, cancellationToken))
        {
            throw new ConflictException(ReturnRules.AlreadyRegistered);
        }

        return BuildPreview(order, payload);
    }

    public async Task<Result> ExecuteAsync(string payloadJson, ApprovalContext context, CancellationToken cancellationToken)
    {
        RegisterReturnPayload payload;
        try
        {
            payload = RegisterReturnPayload.Parse(payloadJson, payloadValidator);
        }
        catch (CommandValidationException exception)
        {
            return Result.Failure(string.Join(" ", exception.Errors.SelectMany(error => error.Value)));
        }

        // A retry of a request whose first execution already saved the record.
        if (await ExistsForApprovalAsync(context.ApprovalRequestId, cancellationToken))
        {
            return Result.Success();
        }

        PortalOrder? order;
        try
        {
            order = await portalClient.GetOrderAsync(payload.OrderId, cancellationToken);
        }
        catch (Exception exception) when (exception is PortalUnavailableException or PortalBusyException)
        {
            return Result.Failure(exception.Message);
        }

        if (order is null)
        {
            return Result.Failure(ReturnRules.OrderNotFound);
        }

        var ruleFailure = ReturnRules.CheckOrderEligibility(order)
            ?? ReturnRules.CheckReturnDate(payload.ReturnDate!.Value, order);
        if (ruleFailure is not null)
        {
            return Result.Failure(ruleFailure);
        }

        if (await dbContext.ReturnRecords.AnyAsync(record => record.OrderId == order.OrderId, cancellationToken))
        {
            return Result.Failure(ReturnRules.AlreadyRegistered);
        }

        var record = ReturnRecord.Create(
            context.ApprovalRequestId,
            order,
            payload.Reason!,
            payload.ReturnDate!.Value,
            context.RequestedByUserId,
            context.RequestedAtUtc,
            context.ReviewedByUserId,
            context.ReviewedAtUtc);

        dbContext.ReturnRecords.Add(record);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // One of the two unique indexes fired: either this same approval was
            // saved concurrently (fine) or another approval for this order won.
            dbContext.Entry(record).State = EntityState.Detached;
            return await ExistsForApprovalAsync(context.ApprovalRequestId, cancellationToken)
                ? Result.Success()
                : Result.Failure(ReturnRules.AlreadyRegistered);
        }

        return Result.Success();
    }

    private Task<bool> ExistsForApprovalAsync(Guid approvalRequestId, CancellationToken cancellationToken) =>
        dbContext.ReturnRecords.AnyAsync(record => record.ApprovalRequestId == approvalRequestId, cancellationToken);

    private static ApprovalPreview BuildPreview(PortalOrder order, RegisterReturnPayload payload)
    {
        var items = order.Items.Count == 0
            ? null
            : string.Join("، ", order.Items.Select(item =>
                $"{item.Title ?? "بدون عنوان"} × {item.Quantity.ToString(CultureInfo.InvariantCulture)}"));

        return new ApprovalPreview($"سفارش {order.OrderId.ToString(CultureInfo.InvariantCulture)}",
        [
            new ApprovalPreviewField("نام مشتری", order.CustomerName),
            new ApprovalPreviewField("اقلام", items),
            new ApprovalPreviewField("مبلغ کل (تومان)", order.Total?.ToString(CultureInfo.InvariantCulture)),
            new ApprovalPreviewField("علت مرجوعی", payload.Reason),
            new ApprovalPreviewField("تاریخ عودت", ReturnRules.FormatJalali(payload.ReturnDate!.Value)),
        ]);
    }
}
