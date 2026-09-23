import { apiFetch } from "@/shared/lib/api-client";

import type {
  ApprovalDecisionResult,
  ApprovalRequestDetail,
  ApprovalRequestListResult,
} from "./approvals-types";

export const approvalsQueryKeys = {
  all: ["approvals"] as const,
  list: (params: ListApprovalsParams) => [...approvalsQueryKeys.all, "list", params] as const,
  detail: (id: string) => [...approvalsQueryKeys.all, "detail", id] as const,
  target: (targetEntityType: string, targetEntityId: string) =>
    [...approvalsQueryKeys.all, "target", targetEntityType, targetEntityId] as const,
};

export interface ListApprovalsParams {
  status?: string;
  type?: string;
  mine?: boolean;
  targetEntityType?: string;
  targetEntityId?: string;
  cursor?: string;
}

export function listApprovals(params: ListApprovalsParams) {
  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  if (params.type) query.set("type", params.type);
  if (params.mine) query.set("mine", "true");
  if (params.targetEntityType) query.set("targetEntityType", params.targetEntityType);
  if (params.targetEntityId) query.set("targetEntityId", params.targetEntityId);
  if (params.cursor) query.set("cursor", params.cursor);

  return apiFetch<ApprovalRequestListResult>(`/api/v1/approvals?${query.toString()}`);
}

export function getApprovalRequest(id: string) {
  return apiFetch<ApprovalRequestDetail>(`/api/v1/approvals/${encodeURIComponent(id)}`);
}

export interface CreateApprovalRequestInput {
  requestType: string;
  targetEntityType: string;
  targetEntityId: string;
  reason: string;
  payload: Record<string, unknown>;
}

export function createApprovalRequest(input: CreateApprovalRequestInput) {
  return apiFetch<{ id: string; requestType: string; status: string; requestedAt: string; expiresAt: string | null }>(
    "/api/v1/approvals",
    { method: "POST", body: JSON.stringify(input) },
  );
}

export function approveApprovalRequest(id: string, note?: string) {
  return apiFetch<ApprovalDecisionResult>(`/api/v1/approvals/${encodeURIComponent(id)}/approval`, {
    method: "POST",
    body: JSON.stringify({ note: note ?? null }),
  });
}

export function rejectApprovalRequest(id: string, note: string) {
  return apiFetch<ApprovalDecisionResult>(`/api/v1/approvals/${encodeURIComponent(id)}/rejection`, {
    method: "POST",
    body: JSON.stringify({ note }),
  });
}

export function cancelApprovalRequest(id: string) {
  return apiFetch<ApprovalDecisionResult>(`/api/v1/approvals/${encodeURIComponent(id)}/cancellation`, {
    method: "POST",
  });
}

export function retryApprovalRequest(id: string) {
  return apiFetch<ApprovalDecisionResult>(`/api/v1/approvals/${encodeURIComponent(id)}/retry`, {
    method: "POST",
  });
}
