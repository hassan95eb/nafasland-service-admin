"use client";

import Link from "next/link";
import { useState } from "react";

import { ApprovalRequestRow } from "@/features/approvals/components/approval-request-row";
import { useApprovalsList } from "@/features/approvals/hooks/use-approvals";
import { returnRequestType } from "@/features/returns/api/returns-api";
import { ReturnDetail } from "@/features/returns/components/return-detail";
import { ReturnsTable } from "@/features/returns/components/returns-table";
import { useReturns } from "@/features/returns/hooks/use-returns";
import { presentApiError } from "@/shared/lib/api-client";
import { LoadMore } from "@/shared/data-table/load-more";
import { Can } from "@/shared/permissions/permission-context";
import { Alert } from "@/shared/ui/alert";

type Tab = "approved" | "requests";

/** ADR-054: approved returns, plus the user's own pending/rejected return requests from Approvals. */
export function ReturnsView() {
  const [tab, setTab] = useState<Tab>("approved");

  return (
    <section className="space-y-5">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1">
          <h1 className="text-2xl font-black">مرجوعی‌ها</h1>
          <p className="text-sm text-[var(--muted)]">مرجوعی‌ها پس از تأیید سوپرادمین در این فهرست ثبت می‌شوند.</p>
        </div>
        <Can permission="returns.request">
          <Link
            className="inline-flex min-h-10 items-center rounded-lg bg-[var(--primary)] px-4 py-2 text-sm font-bold text-white hover:bg-[var(--primary-strong)]"
            href="/returns/new"
          >
            ثبت مرجوعی
          </Link>
        </Can>
      </header>

      <div className="flex gap-2 border-b border-[var(--border)]" role="tablist">
        <TabButton active={tab === "approved"} onClick={() => setTab("approved")}>تأییدشده</TabButton>
        <TabButton active={tab === "requests"} onClick={() => setTab("requests")}>در انتظار / ردشده</TabButton>
      </div>

      {tab === "approved" ? <ApprovedReturns /> : <MyReturnRequests />}
    </section>
  );
}

function TabButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: string }) {
  return (
    <button
      type="button"
      role="tab"
      aria-selected={active}
      className={`-mb-px border-b-2 px-4 py-2 text-sm font-bold ${active ? "border-[var(--primary)] text-[var(--foreground)]" : "border-transparent text-[var(--muted)] hover:text-[var(--foreground)]"}`}
      onClick={onClick}
    >
      {children}
    </button>
  );
}

function ApprovedReturns() {
  const query = useReturns();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const items = query.data?.pages.flatMap((page) => page.items) ?? [];

  return (
    <>
      <div className="overflow-hidden rounded-xl border border-[var(--border)] bg-white shadow-sm">
        {query.isPending ? (
          <p className="py-12 text-center text-sm text-[var(--muted)]">در حال دریافت مرجوعی‌ها…</p>
        ) : query.isError && items.length === 0 ? (
          <div className="p-4"><Alert>{presentApiError(query.error)}</Alert></div>
        ) : items.length === 0 ? (
          <p className="px-4 py-12 text-center text-sm text-[var(--muted)]">هنوز مرجوعی‌ای ثبت نشده</p>
        ) : (
          <>
            <ReturnsTable items={items} selectedId={selectedId} onSelect={setSelectedId} />
            {query.isError ? <div className="p-4"><Alert>{presentApiError(query.error)}</Alert></div> : null}
            <LoadMore hasMore={query.hasNextPage} loading={query.isFetchingNextPage} onLoadMore={() => void query.fetchNextPage()} />
          </>
        )}
      </div>

      {selectedId ? <ReturnDetail id={selectedId} onClose={() => setSelectedId(null)} /> : null}
    </>
  );
}

/** The existing Approvals rows (cancel while Pending, review note once rejected) — reused, not copied. */
function MyReturnRequests() {
  const query = useApprovalsList({ mine: true, type: returnRequestType });

  if (query.isPending) {
    return <p className="py-12 text-center text-sm text-[var(--muted)]">در حال دریافت درخواست‌ها…</p>;
  }
  if (query.error) {
    return <Alert>{presentApiError(query.error)}</Alert>;
  }

  // Executed requests already appear as records in the «تأییدشده» tab.
  const items = (query.data?.items ?? []).filter((request) => request.status !== "Executed");
  return items.length === 0 ? (
    <p className="rounded-xl border border-[var(--border)] bg-white p-5 text-sm text-[var(--muted)]">درخواست در انتظار یا ردشده‌ای ندارید.</p>
  ) : (
    <div className="space-y-3">
      {items.map((request) => <ApprovalRequestRow key={request.id} request={request} canReview={false} />)}
    </div>
  );
}
