"use client";

import { useInfiniteQuery, useQuery, useQueryClient } from "@tanstack/react-query";

import { getOrderPreview, getReturn, listReturns, returnsQueryKeys } from "@/features/returns/api/returns-api";

/** Fetched only on "دریافت سفارش", never while typing — every call is a live portal read behind the shared rate limiter. */
export function useOrderPreview(orderId: string | null) {
  return useQuery({
    queryKey: returnsQueryKeys.order(orderId ?? ""),
    queryFn: () => getOrderPreview(orderId!),
    enabled: Boolean(orderId),
    retry: false,
    staleTime: 0,
  });
}

// Keyset pagination (ADR-014): "load more" sends the previous page's nextCursor.
export function useReturns() {
  return useInfiniteQuery({
    queryKey: returnsQueryKeys.list(),
    queryFn: ({ pageParam }) => listReturns(pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  });
}

export function useReturn(id: string | null) {
  return useQuery({
    queryKey: returnsQueryKeys.detail(id ?? ""),
    queryFn: () => getReturn(id!),
    enabled: Boolean(id),
  });
}

/** After filing: no optimistic update (the portal and the reviewer are the source of truth), only invalidation. */
export function useInvalidateReturns() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: returnsQueryKeys.all });
}
