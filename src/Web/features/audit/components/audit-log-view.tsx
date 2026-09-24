"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useMemo, useState } from "react";

import { AuditEventDetail } from "@/features/audit/components/audit-event-detail";
import { AuditExportButton } from "@/features/audit/components/audit-export-button";
import { AuditFiltersForm } from "@/features/audit/components/audit-filters-form";
import { AuditLimitationNotice } from "@/features/audit/components/audit-limitation-notice";
import { AuditLogTable } from "@/features/audit/components/audit-log-table";
import { useAuditLogs } from "@/features/audit/hooks/use-audit";
import { parseAuditFilters, serializeAuditFilters, type AuditFilters } from "@/features/audit/lib/audit-filters";
import { presentApiError } from "@/shared/lib/api-client";
import { LoadMore } from "@/shared/data-table/load-more";
import { Can } from "@/shared/permissions/permission-context";
import { Alert } from "@/shared/ui/alert";

/** ADR-014, view 1: the global log. Filters live in the URL so a view can be shared and survives a refresh. */
export function AuditLogView() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const searchKey = searchParams.toString();
  const filters = useMemo(() => parseAuditFilters(new URLSearchParams(searchKey)), [searchKey]);
  const query = useAuditLogs(filters);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const closeDetail = useCallback(() => setSelectedId(null), []);

  function applyFilters(next: AuditFilters) {
    const params = serializeAuditFilters(next).toString();
    router.replace(params ? `${pathname}?${params}` : pathname);
  }

  const items = query.data?.pages.flatMap((page) => page.items) ?? [];

  return (
    <section className="space-y-5">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1">
          <h1 className="text-2xl font-black">گزارش فعالیت</h1>
          <p className="text-sm text-[var(--muted)]">همهٔ رویدادهای ثبت‌شده از این پنل، از جدیدترین.</p>
        </div>
        <Can permission="audit.export">
          <AuditExportButton filters={filters} />
        </Can>
      </header>

      <AuditLimitationNotice />

      {/* Keyed by the URL so a back/forward navigation resets the draft to what the URL says. */}
      <AuditFiltersForm key={searchKey} value={filters} onApply={applyFilters} />

      <div className="overflow-hidden rounded-xl border border-[var(--border)] bg-white shadow-sm">
        {query.isPending ? (
          <p className="py-12 text-center text-sm text-[var(--muted)]">در حال دریافت گزارش…</p>
        ) : query.isError && items.length === 0 ? (
          <div className="p-4"><Alert>{presentApiError(query.error)}</Alert></div>
        ) : items.length === 0 ? (
          <p className="px-4 py-12 text-center text-sm text-[var(--muted)]">هیچ رویدادی در این بازه ثبت نشده.</p>
        ) : (
          <>
            <AuditLogTable items={items} selectedId={selectedId} onSelect={setSelectedId} />
            {query.isError ? <div className="p-4"><Alert>{presentApiError(query.error)}</Alert></div> : null}
            <LoadMore hasMore={query.hasNextPage} loading={query.isFetchingNextPage} onLoadMore={() => void query.fetchNextPage()} />
          </>
        )}
      </div>

      {selectedId ? <AuditEventDetail id={selectedId} onClose={closeDetail} /> : null}
    </section>
  );
}
