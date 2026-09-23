"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import {
  approvalsQueryKeys,
  approveApprovalRequest,
  cancelApprovalRequest,
  createApprovalRequest,
  getApprovalRequest,
  listApprovals,
  rejectApprovalRequest,
  retryApprovalRequest,
  type CreateApprovalRequestInput,
  type ListApprovalsParams,
} from "@/features/approvals/api/approvals-api";

export function useApprovalsList(params: ListApprovalsParams) {
  return useQuery({ queryKey: approvalsQueryKeys.list(params), queryFn: () => listApprovals(params) });
}

/** The "is there a Pending request on me?" badge lookup used by product-details.tsx. */
export function usePendingApprovalForTarget(targetEntityType: string, targetEntityId: string | null) {
  return useQuery({
    queryKey: approvalsQueryKeys.target(targetEntityType, targetEntityId ?? ""),
    queryFn: () => listApprovals({ targetEntityType, targetEntityId: targetEntityId!, status: "Pending" }),
    enabled: Boolean(targetEntityId),
  });
}

export function useApprovalRequest(id: string) {
  return useQuery({ queryKey: approvalsQueryKeys.detail(id), queryFn: () => getApprovalRequest(id) });
}

function useInvalidateApprovals() {
  const queryClient = useQueryClient();
  return (id?: string) => {
    void queryClient.invalidateQueries({ queryKey: approvalsQueryKeys.all });
    if (id) {
      void queryClient.invalidateQueries({ queryKey: approvalsQueryKeys.detail(id) });
    }
  };
}

export function useCreateApprovalRequest() {
  const invalidate = useInvalidateApprovals();
  return useMutation({
    mutationFn: (input: CreateApprovalRequestInput) => createApprovalRequest(input),
    onSuccess: () => invalidate(),
  });
}

export function useApproveApprovalRequest() {
  const invalidate = useInvalidateApprovals();
  return useMutation({
    mutationFn: ({ id, note }: { id: string; note?: string }) => approveApprovalRequest(id, note),
    onSuccess: (_, variables) => invalidate(variables.id),
  });
}

export function useRejectApprovalRequest() {
  const invalidate = useInvalidateApprovals();
  return useMutation({
    mutationFn: ({ id, note }: { id: string; note: string }) => rejectApprovalRequest(id, note),
    onSuccess: (_, variables) => invalidate(variables.id),
  });
}

export function useCancelApprovalRequest() {
  const invalidate = useInvalidateApprovals();
  return useMutation({
    mutationFn: (id: string) => cancelApprovalRequest(id),
    onSuccess: (_, id) => invalidate(id),
  });
}

export function useRetryApprovalRequest() {
  const invalidate = useInvalidateApprovals();
  return useMutation({
    mutationFn: (id: string) => retryApprovalRequest(id),
    onSuccess: (_, id) => invalidate(id),
  });
}
