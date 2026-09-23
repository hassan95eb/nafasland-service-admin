"use client";

import { useApprovalsList } from "@/features/approvals/hooks/use-approvals";
import { ApprovalRequestRow } from "@/features/approvals/components/approval-request-row";
import { presentApiError } from "@/shared/lib/api-client";
import { Alert } from "@/shared/ui/alert";

export function MyApprovalRequests() {
  const query = useApprovalsList({ mine: true });

  if (query.isPending) {
    return <p className="py-12 text-center text-sm text-[var(--muted)]">در حال دریافت درخواست‌ها…</p>;
  }
  if (query.error) {
    return <Alert>{presentApiError(query.error)}</Alert>;
  }

  const items = query.data?.items ?? [];
  return (
    <section className="space-y-4">
      <h1 className="text-2xl font-black">درخواست‌های من</h1>
      {items.length === 0 ? (
        <p className="rounded-xl border border-[var(--border)] bg-white p-5 text-sm text-[var(--muted)]">هنوز درخواستی ثبت نکرده‌اید.</p>
      ) : (
        items.map((request) => <ApprovalRequestRow key={request.id} request={request} canReview={false} />)
      )}
    </section>
  );
}
