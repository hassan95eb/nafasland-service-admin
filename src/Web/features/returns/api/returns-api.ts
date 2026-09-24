import type { components } from "@/shared/lib/api-types.generated";
import { apiFetch } from "@/shared/lib/api-client";

export type OrderPreview = components["schemas"]["OrderPreviewDto"];
export type OrderItem = components["schemas"]["OrderItemDto"];
export type ReturnRecordSummary = components["schemas"]["ReturnRecordSummaryDto"];
export type ReturnRecordPage = components["schemas"]["ReturnRecordPageDto"];
export type ReturnRecordDetail = components["schemas"]["ReturnRecordDetailDto"];

/** The Approvals request type the Returns module registers its executor under (ADR-054). */
export const returnRequestType = "returns.register";
export const returnTargetEntityType = "Order";

export const returnsQueryKeys = {
  all: ["returns"] as const,
  order: (orderId: string) => [...returnsQueryKeys.all, "order", orderId] as const,
  list: () => [...returnsQueryKeys.all, "list"] as const,
  detail: (id: string) => [...returnsQueryKeys.all, "detail", id] as const,
};

export function getOrderPreview(orderId: string) {
  return apiFetch<OrderPreview>(`/api/v1/returns/orders/${encodeURIComponent(orderId)}`);
}

export function listReturns(cursor?: string) {
  return apiFetch<ReturnRecordPage>(cursor ? `/api/v1/returns?cursor=${encodeURIComponent(cursor)}` : "/api/v1/returns");
}

export function getReturn(id: string) {
  return apiFetch<ReturnRecordDetail>(`/api/v1/returns/${encodeURIComponent(id)}`);
}
