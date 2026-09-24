"use client";

import { useCallback, useState } from "react";

import { AuditEventDetail } from "@/features/audit/components/audit-event-detail";
import { AuditLogTable } from "@/features/audit/components/audit-log-table";
import { useProductAuditHistory } from "@/features/audit/hooks/use-audit";
import { presentApiError } from "@/shared/lib/api-client";
import { formatPersianDateTime } from "@/shared/lib/formatters";
import { LoadMore } from "@/shared/data-table/load-more";
import { Alert } from "@/shared/ui/alert";

/**
 * ADR-014, view 4. Rendered only inside <Can permission="audit.read.all">, so
 * without that permission the request is never even sent. The product's current
 * variant ids go along so variant changes recorded before the parent-product
 * link existed (and variant-delete requests) are found too.
 */
export function ProductAuditHistory({ productId, variantIds }: { productId: string; variantIds: readonly string[] }) {
  const query = useProductAuditHistory(productId, variantIds);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const closeDetail = useCallback(() => setSelectedId(null), []);

  const firstPage = query.data?.pages[0];
  const items = query.data?.pages.flatMap((page) => page.items) ?? [];

  return (
    <section className="space-y-3">
      <h2 className="text-lg font-black">تاریخچهٔ تغییرات</h2>
      {firstPage?.lastKnownTitle ? (
        <p className="text-sm text-[var(--muted)]">
          آخرین عنوان ثبت‌شده: <strong className="text-[var(--foreground)]">{firstPage.lastKnownTitle}</strong>
          {firstPage.lastSyncedAt ? ` (${formatPersianDateTime(firstPage.lastSyncedAt)})` : null}
        </p>
      ) : null}
      <p className="text-xs leading-6 text-[var(--muted)]">
        فقط کارهای انجام‌شده از همین پنل. تغییرات قیمت و موجودی و درخواست‌های حذف واریانت‌هایی که پیش‌تر حذف شده‌اند و
        پیش از این نسخه ثبت شده‌اند، فقط در گزارش فعالیت سراسری دیده می‌شوند.
      </p>

      <div className="overflow-hidden rounded-xl border border-[var(--border)] bg-white shadow-sm">
        {query.isPending ? (
          <p className="py-8 text-center text-sm text-[var(--muted)]">در حال دریافت تاریخچه…</p>
        ) : query.isError && items.length === 0 ? (
          <div className="p-4"><Alert>{presentApiError(query.error)}</Alert></div>
        ) : items.length === 0 ? (
          <p className="px-4 py-8 text-center text-sm text-[var(--muted)]">هیچ رویدادی برای این محصول ثبت نشده.</p>
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
