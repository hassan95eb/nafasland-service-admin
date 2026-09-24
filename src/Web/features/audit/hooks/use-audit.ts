"use client";

import { useInfiniteQuery, useQuery } from "@tanstack/react-query";

import {
  auditQueryKeys,
  getAuditExportStatus,
  getAuditLog,
  getProductAuditHistory,
  getUserActivity,
  listAuditActors,
  listAuditLogs,
} from "@/features/audit/api/audit-api";
import type { AuditFilters } from "@/features/audit/lib/audit-filters";

// Keyset pagination (ADR-014): each "load more" sends the previous page's
// nextCursor; there is no page number or offset anywhere.

export function useAuditLogs(filters: AuditFilters) {
  return useInfiniteQuery({
    queryKey: auditQueryKeys.logs(filters),
    queryFn: ({ pageParam }) => listAuditLogs(filters, pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  });
}

export function useAuditLog(id: string | null) {
  return useQuery({
    queryKey: auditQueryKeys.detail(id ?? ""),
    queryFn: () => getAuditLog(id!),
    enabled: Boolean(id),
  });
}

export function useUserActivity(userId: string, range: { from?: string; to?: string }) {
  return useInfiniteQuery({
    queryKey: auditQueryKeys.user(userId, range.from, range.to),
    queryFn: ({ pageParam }) => getUserActivity(userId, range, pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.timeline.nextCursor ?? undefined,
  });
}

export function useProductAuditHistory(productId: string, variantIds: readonly string[]) {
  return useInfiniteQuery({
    queryKey: auditQueryKeys.product(productId, variantIds),
    queryFn: ({ pageParam }) => getProductAuditHistory(productId, variantIds, pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  });
}

export function useAuditActors() {
  return useQuery({ queryKey: auditQueryKeys.actors(), queryFn: listAuditActors, staleTime: 60_000 });
}

const exportPollIntervalMs = 5_000;

/** Polls a background export until it finishes; never faster than every 5 seconds. */
export function useAuditExportStatus(jobId: string | null) {
  return useQuery({
    queryKey: auditQueryKeys.exportStatus(jobId ?? ""),
    queryFn: () => getAuditExportStatus(jobId!),
    enabled: Boolean(jobId),
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status === "Completed" || status === "Failed" ? false : exportPollIntervalMs;
    },
  });
}
