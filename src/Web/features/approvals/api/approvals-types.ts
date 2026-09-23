// These shapes are hand-written rather than pulled from
// `@/shared/lib/api-types.generated` (ADR-011's normal convention) because that
// file is generated from the backend's live OpenAPI document and this step did
// not have a running backend to regenerate it against. Run
// `npm run generate:api-types` once the backend is up and switch these imports
// over to `components["schemas"][...]`, the same way every other feature does.

export type ApprovalRequestStatus =
  | "Pending"
  | "Approved"
  | "Rejected"
  | "Executed"
  | "ExecutionFailed"
  | "Cancelled"
  | "Expired";

export interface ApprovalPreviewField {
  label: string;
  value: string | null;
}

export interface ApprovalPreview {
  entityTitle: string;
  fields: ApprovalPreviewField[];
}

export interface ApprovalRequestSummary {
  id: string;
  requestType: string;
  targetEntityType: string;
  targetEntityId: string;
  reason: string;
  status: ApprovalRequestStatus;
  requestedByUserId: string;
  requestedAt: string;
  reviewedByUserId: string | null;
  reviewedAt: string | null;
  expiresAt: string | null;
}

export interface ApprovalRequestDetail extends ApprovalRequestSummary {
  reviewNote: string | null;
  executedAt: string | null;
  executionError: string | null;
  preview: ApprovalPreview | null;
}

export interface ApprovalRequestListResult {
  items: ApprovalRequestSummary[];
  nextCursor: string | null;
}

export interface ApprovalDecisionResult {
  id: string;
  status: ApprovalRequestStatus;
  executionError: string | null;
}
