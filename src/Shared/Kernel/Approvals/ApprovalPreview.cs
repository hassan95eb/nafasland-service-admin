namespace NafasLand.Admin.Shared.Kernel.Approvals;

/// <summary>
/// What a reviewer sees before deciding on a request (ADR-010, rule 5). Built by
/// <see cref="IApprovalExecutor.PreviewAsync"/> from a live read of the target
/// entity, never from the request's stored <c>SnapshotJson</c> — the whole point
/// is to show what the state actually is right now, not what it was when the
/// request was filed.
/// </summary>
public sealed record ApprovalPreview(string EntityTitle, IReadOnlyList<ApprovalPreviewField> Fields);

public sealed record ApprovalPreviewField(string Label, string? Value);
