import type { components } from "@/shared/lib/api-types.generated";
import { apiFetch, apiFetchResponse } from "@/shared/lib/api-client";

import { toApiFilters, type AuditFilters } from "@/features/audit/lib/audit-filters";

export type AuditLogSummary = components["schemas"]["AuditLogSummaryDto"];
export type AuditLogPage = components["schemas"]["AuditLogPageDto"];
export type AuditLogDetail = components["schemas"]["AuditLogDetailDto"];
export type UserActivity = components["schemas"]["UserActivityDto"];
export type ProductAuditHistory = components["schemas"]["ProductAuditHistoryDto"];
export type AuditActor = components["schemas"]["AuditActorDto"];
export type AuditExportStatus = components["schemas"]["AuditExportStatusDto"];
type ExportAuditLogRequest = components["schemas"]["ExportAuditLogRequest"];
type AuditExportAccepted = components["schemas"]["AuditExportAcceptedDto"];

export type ExportFormat = "csv" | "xlsx";

export const auditQueryKeys = {
  all: ["audit"] as const,
  logs: (filters: AuditFilters) => [...auditQueryKeys.all, "logs", filters] as const,
  detail: (id: string) => [...auditQueryKeys.all, "detail", id] as const,
  user: (userId: string, from?: string, to?: string) => [...auditQueryKeys.all, "user", userId, from, to] as const,
  product: (productId: string, variantIds: readonly string[]) =>
    [...auditQueryKeys.all, "product", productId, variantIds] as const,
  actors: () => [...auditQueryKeys.all, "actors"] as const,
  exportStatus: (jobId: string) => [...auditQueryKeys.all, "export", jobId] as const,
};

function withParams(path: string, params: Record<string, string | null | undefined>) {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value) query.set(key, value);
  }
  const text = query.toString();
  return text ? `${path}?${text}` : path;
}

export function listAuditLogs(filters: AuditFilters, cursor?: string) {
  return apiFetch<AuditLogPage>(withParams("/api/v1/audit/logs", { ...toApiFilters(filters), cursor }));
}

export function getAuditLog(id: string) {
  return apiFetch<AuditLogDetail>(`/api/v1/audit/logs/${encodeURIComponent(id)}`);
}

export function getUserActivity(userId: string, range: { from?: string; to?: string }, cursor?: string) {
  const { from, to } = toApiFilters(range);
  return apiFetch<UserActivity>(withParams(`/api/v1/audit/users/${encodeURIComponent(userId)}`, { from, to, cursor }));
}

export function getProductAuditHistory(productId: string, variantIds: readonly string[], cursor?: string) {
  const query = new URLSearchParams();
  for (const variantId of variantIds) query.append("variantId", variantId);
  if (cursor) query.set("cursor", cursor);
  const text = query.toString();
  return apiFetch<ProductAuditHistory>(
    `/api/v1/audit/products/${encodeURIComponent(productId)}${text ? `?${text}` : ""}`,
  );
}

export function listAuditActors() {
  return apiFetch<AuditActor[]>("/api/v1/audit/actors");
}

export function getAuditExportStatus(jobId: string) {
  return apiFetch<AuditExportStatus>(`/api/v1/audit/export/${encodeURIComponent(jobId)}/status`);
}

export function auditExportDownloadUrl(jobId: string) {
  return `/api/v1/audit/export/${encodeURIComponent(jobId)}/download`;
}

export type StartExportResult =
  | { kind: "file"; blob: Blob; fileName: string }
  | { kind: "job"; jobId: string };

/**
 * POST /audit/export answers with the file itself (≤ 25,000 rows) or 202 with
 * a job id — so this reads the raw response instead of apiFetch's JSON.
 */
export async function startAuditExport(filters: AuditFilters, format: ExportFormat): Promise<StartExportResult> {
  const body: ExportAuditLogRequest = { ...toApiFilters(filters), format };
  const response = await apiFetchResponse("/api/v1/audit/export", { method: "POST", body: JSON.stringify(body) });

  if (response.status === 202) {
    const accepted = (await response.json()) as AuditExportAccepted;
    return { kind: "job", jobId: accepted.jobId };
  }

  const disposition = response.headers.get("content-disposition") ?? "";
  const fileName = /filename="?([^";]+)"?/i.exec(disposition)?.[1] ?? `audit-log.${format}`;
  return { kind: "file", blob: await response.blob(), fileName };
}
