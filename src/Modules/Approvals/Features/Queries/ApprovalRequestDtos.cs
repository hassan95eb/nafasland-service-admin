using NafasLand.Admin.Modules.Approvals.Persistence;
using NafasLand.Admin.Shared.Kernel.Approvals;

namespace NafasLand.Admin.Modules.Approvals.Features.Queries;

internal sealed record ApprovalRequestSummaryDto(
    Guid Id,
    string RequestType,
    string TargetEntityType,
    string TargetEntityId,
    string Reason,
    string Status,
    Guid RequestedByUserId,
    DateTime RequestedAt,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAt,
    DateTime? ExpiresAt)
{
    public static ApprovalRequestSummaryDto FromEntity(ApprovalRequest request) => new(
        request.Id,
        request.RequestType,
        request.TargetEntityType,
        request.TargetEntityId,
        request.Reason,
        request.Status.ToString(),
        request.RequestedByUserId,
        request.RequestedAt,
        request.ReviewedByUserId,
        request.ReviewedAt,
        request.ExpiresAt);
}

internal sealed record ApprovalRequestDetailDto(
    Guid Id,
    string RequestType,
    string TargetEntityType,
    string TargetEntityId,
    string Reason,
    string Status,
    Guid RequestedByUserId,
    DateTime RequestedAt,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAt,
    string? ReviewNote,
    DateTime? ExecutedAt,
    string? ExecutionError,
    DateTime? ExpiresAt,
    ApprovalPreview? Preview)
{
    public static ApprovalRequestDetailDto FromEntity(ApprovalRequest request, ApprovalPreview? preview) => new(
        request.Id,
        request.RequestType,
        request.TargetEntityType,
        request.TargetEntityId,
        request.Reason,
        request.Status.ToString(),
        request.RequestedByUserId,
        request.RequestedAt,
        request.ReviewedByUserId,
        request.ReviewedAt,
        request.ReviewNote,
        request.ExecutedAt,
        request.ExecutionError,
        request.ExpiresAt,
        preview);
}
