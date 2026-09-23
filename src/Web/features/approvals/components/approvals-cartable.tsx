"use client";

import { useState } from "react";

import { useApprovalsList } from "@/features/approvals/hooks/use-approvals";
import { ApprovalRequestRow } from "@/features/approvals/components/approval-request-row";
import { presentApiError } from "@/shared/lib/api-client";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";

const statusFilters = [
  { value: "Pending", label: "در انتظار" },
  { value: "ExecutionFailed", label: "اجرا ناموفق" },
  { value: undefined, label: "همه" },
] as const;

/** SuperAdmin's full cartable (approvals.read.all) — ADR-011. */
export function ApprovalsCartable() {
  const [status, setStatus] = useState<string | undefined>("Pending");
  const query = useApprovalsList({ status });

  if (query.isPending) {
    return <p className="py-12 text-center text-sm text-[var(--muted)]">در حال دریافت کارتابل…</p>;
  }
  if (query.error) {
    return <Alert>{presentApiError(query.error)}</Alert>;
  }

  const items = query.data?.items ?? [];
  return (
    <section className="space-y-4">
      <h1 className="text-2xl font-black">کارتابل تأیید</h1>
      <div className="flex gap-2">
        {statusFilters.map((filter) => (
          <Button
            key={filter.label}
            type="button"
            variant={status === filter.value ? "primary" : "secondary"}
            onClick={() => setStatus(filter.value)}
          >
            {filter.label}
          </Button>
        ))}
      </div>
      {items.length === 0 ? (
        <p className="rounded-xl border border-[var(--border)] bg-white p-5 text-sm text-[var(--muted)]">درخواستی در این وضعیت نیست.</p>
      ) : (
        items.map((request) => <ApprovalRequestRow key={request.id} request={request} canReview />)
      )}
    </section>
  );
}
