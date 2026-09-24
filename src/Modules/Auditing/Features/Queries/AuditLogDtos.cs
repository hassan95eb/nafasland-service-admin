using System.Text.Json;
using NafasLand.Admin.Modules.Auditing.Persistence;
using NafasLand.Admin.Shared.Kernel.Users;

namespace NafasLand.Admin.Modules.Auditing.Features.Queries;

/// <summary>
/// ActorUsername is null in two distinct cases the UI labels differently: a
/// system record (ActorUserId itself null — AuditPurged, ApprovalExpired) and an
/// unknown/removed user (ActorUserId set, but no longer in Identity).
/// </summary>
internal sealed record AuditLogSummaryDto(
    Guid Id,
    string CorrelationId,
    Guid? ActorUserId,
    string? ActorUsername,
    string ActorRoleAtTime,
    string Action,
    string? EntityType,
    string? EntityId,
    string Outcome,
    string? FailureReason,
    DateTime CreatedAt)
{
    public static AuditLogSummaryDto FromEntity(AuditLog log, IReadOnlyDictionary<Guid, string> usernames) => new(
        log.Id,
        log.CorrelationId,
        log.ActorUserId,
        UsernameOf(log.ActorUserId, usernames),
        log.ActorRoleAtTime,
        log.Action,
        log.EntityType,
        log.EntityId,
        log.Outcome.ToString(),
        log.FailureReason,
        log.CreatedAt);

    public static string? UsernameOf(Guid? userId, IReadOnlyDictionary<Guid, string> usernames) =>
        userId is { } id && usernames.TryGetValue(id, out var username) ? username : null;
}

internal sealed record AuditLogPageDto(IReadOnlyList<AuditLogSummaryDto> Items, string? NextCursor)
{
    public static async Task<AuditLogPageDto> CreateAsync(
        IReadOnlyList<AuditLog> rowsPlusOne,
        int pageSize,
        IUserDirectory userDirectory,
        CancellationToken cancellationToken)
    {
        var hasMore = rowsPlusOne.Count > pageSize;
        var page = hasMore ? rowsPlusOne.Take(pageSize).ToList() : rowsPlusOne;
        var nextCursor = hasMore ? AuditLogQueryExtensions.EncodeCursor(page[^1]) : null;

        var actorIds = page.Where(log => log.ActorUserId.HasValue).Select(log => log.ActorUserId!.Value).Distinct().ToList();
        var usernames = await userDirectory.GetUsernamesAsync(actorIds, cancellationToken);

        return new AuditLogPageDto(page.Select(log => AuditLogSummaryDto.FromEntity(log, usernames)).ToList(), nextCursor);
    }
}

/// <summary>Before/After/ChangedFields go out already parsed, with sensitive keys masked (see SensitiveAuditFields).</summary>
internal sealed record AuditLogDetailDto(
    Guid Id,
    string CorrelationId,
    Guid? ActorUserId,
    string? ActorUsername,
    Guid? OnBehalfOfUserId,
    string? OnBehalfOfUsername,
    string ActorRoleAtTime,
    string Action,
    string? EntityType,
    string? EntityId,
    string? ParentEntityType,
    string? ParentEntityId,
    JsonElement? Before,
    JsonElement? After,
    JsonElement? ChangedFields,
    string Outcome,
    int? UpstreamStatus,
    string? FailureReason,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAt);

internal sealed record OutcomeCountDto(string Outcome, int Count);

/// <summary>Per Action and Outcome, so a denied or failed attempt is never counted as a done "create"/"edit".</summary>
internal sealed record ActionCountDto(string Action, string Outcome, int Count);

internal sealed record UserActivityDto(
    Guid UserId,
    string? Username,
    IReadOnlyList<OutcomeCountDto> CountsByOutcome,
    IReadOnlyList<ActionCountDto> CountsByAction,
    AuditLogPageDto Timeline);

internal sealed record ProductAuditHistoryDto(
    string ExternalProductId,
    string? LastKnownTitle,
    DateTime? LastSyncedAt,
    IReadOnlyList<AuditLogSummaryDto> Items,
    string? NextCursor);

internal sealed record AuditActorDto(Guid UserId, string? Username);
