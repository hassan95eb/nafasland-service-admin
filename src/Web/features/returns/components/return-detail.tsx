"use client";

import { useEffect } from "react";

import { OrderSummary } from "@/features/returns/components/order-summary";
import { userLabel } from "@/features/returns/components/returns-table";
import { useReturn } from "@/features/returns/hooks/use-returns";
import { presentApiError } from "@/shared/lib/api-client";
import { formatPersianDate, formatPersianDateTime } from "@/shared/lib/formatters";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";

/** One approved return in a side panel: the order snapshot, the reason, and who followed it up (registered and approved), with times. */
export function ReturnDetail({ id, onClose }: { id: string; onClose: () => void }) {
  const query = useReturn(id);

  useEffect(() => {
    function closeOnEscape(event: KeyboardEvent) {
      if (event.key === "Escape") onClose();
    }
    window.addEventListener("keydown", closeOnEscape);
    return () => window.removeEventListener("keydown", closeOnEscape);
  }, [onClose]);

  return (
    <div className="fixed inset-0 z-40 flex justify-end bg-black/30" onClick={onClose}>
      <aside
        className="h-full w-full max-w-2xl overflow-y-auto bg-white p-5 shadow-xl sm:p-6"
        role="dialog"
        aria-modal="true"
        aria-label="جزئیات مرجوعی"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="mb-4 flex items-center justify-between gap-3">
          <h2 className="text-lg font-black">مرجوعی سفارش {query.data ? <span dir="ltr">{query.data.orderId}</span> : null}</h2>
          <Button type="button" variant="ghost" onClick={onClose}>بستن</Button>
        </div>

        {query.isPending ? (
          <p className="py-12 text-center text-sm text-[var(--muted)]">در حال دریافت جزئیات…</p>
        ) : query.error || !query.data ? (
          <Alert>{presentApiError(query.error)}</Alert>
        ) : (
          <div className="space-y-5">
            <dl className="grid gap-x-4 gap-y-3 text-sm sm:grid-cols-[10rem_minmax(0,1fr)]">
              <dt className="text-[var(--muted)]">علت مرجوعی</dt>
              <dd className="whitespace-pre-line leading-7">{query.data.reason}</dd>
              <dt className="text-[var(--muted)]">تاریخ عودت</dt>
              <dd>{formatPersianDate(query.data.returnDate)}</dd>
              <dt className="text-[var(--muted)]">ثبت‌کننده</dt>
              <dd>{userLabel(query.data.registeredByUsername)} — {formatPersianDateTime(query.data.registeredAt)}</dd>
              <dt className="text-[var(--muted)]">تأییدکننده</dt>
              <dd>{userLabel(query.data.approvedByUsername)} — {formatPersianDateTime(query.data.approvedAt)}</dd>
            </dl>

            <div className="space-y-2 border-t border-[var(--border)] pt-4">
              <p className="text-xs text-[var(--muted)]">اطلاعات سفارش در لحظهٔ تأیید</p>
              <OrderSummary
                order={{ ...query.data, statuses: query.data.orderStatuses, createdAtUtc: query.data.orderCreatedAtUtc }}
              />
            </div>
          </div>
        )}
      </aside>
    </div>
  );
}
