"use client";

import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useMemo, useState, type FormEvent } from "react";

import { AuditEventDetail } from "@/features/audit/components/audit-event-detail";
import { AuditLimitationNotice } from "@/features/audit/components/audit-limitation-notice";
import { AuditLogTable } from "@/features/audit/components/audit-log-table";
import { useUserActivity } from "@/features/audit/hooks/use-audit";
import { jalaliDaysAgo, parseAuditFilters, parseJalaliDate, serializeAuditFilters, todayJalali } from "@/features/audit/lib/audit-filters";
import { actionLabel, actorDisplayName, outcomeLabel, summarizeActivity } from "@/features/audit/lib/audit-labels";
import { presentApiError } from "@/shared/lib/api-client";
import { formatPersianNumber } from "@/shared/lib/formatters";
import { LoadMore } from "@/shared/data-table/load-more";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";

/** ADR-014, view 2: one admin's counts for the chosen window, plus their timeline. The window lives in the URL. */
export function UserActivityView({ userId }: { userId: string }) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const searchKey = searchParams.toString();
  const { from, to } = useMemo(() => parseAuditFilters(new URLSearchParams(searchKey)), [searchKey]);
  const query = useUserActivity(userId, { from, to });
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const closeDetail = useCallback(() => setSelectedId(null), []);

  function applyRange(range: { from?: string; to?: string }) {
    const params = serializeAuditFilters(range).toString();
    router.replace(params ? `${pathname}?${params}` : pathname);
  }

  const firstPage = query.data?.pages[0];
  const items = query.data?.pages.flatMap((page) => page.timeline.items) ?? [];
  const title = firstPage ? actorDisplayName({ actorUserId: userId, actorUsername: firstPage.username }) : "…";

  return (
    <section className="space-y-5">
      <Link className="text-sm font-bold text-[var(--primary)]" href="/admin/audit">بازگشت به گزارش فعالیت</Link>
      <header className="space-y-1">
        <h1 className="text-2xl font-black">فعالیت {title}</h1>
        <p className="text-xs text-[var(--muted)]" dir="ltr">{userId}</p>
      </header>

      <AuditLimitationNotice />

      <RangeForm key={searchKey} from={from} to={to} onApply={applyRange} />

      {query.isPending ? (
        <p className="py-12 text-center text-sm text-[var(--muted)]">در حال دریافت فعالیت…</p>
      ) : query.isError && !firstPage ? (
        <Alert>{presentApiError(query.error)}</Alert>
      ) : firstPage ? (
        <>
          <ActivityCards countsByAction={firstPage.countsByAction} />

          <div className="overflow-hidden rounded-xl border border-[var(--border)] bg-white shadow-sm">
            <h2 className="border-b border-[var(--border)] px-4 py-3 font-black">تایم‌لاین</h2>
            {items.length === 0 ? (
              <p className="px-4 py-12 text-center text-sm text-[var(--muted)]">هیچ رویدادی در این بازه ثبت نشده.</p>
            ) : (
              <>
                <AuditLogTable items={items} selectedId={selectedId} onSelect={setSelectedId} showActor={false} />
                {query.isError ? <div className="p-4"><Alert>{presentApiError(query.error)}</Alert></div> : null}
                <LoadMore hasMore={query.hasNextPage} loading={query.isFetchingNextPage} onLoadMore={() => void query.fetchNextPage()} />
              </>
            )}
          </div>
        </>
      ) : null}

      {selectedId ? <AuditEventDetail id={selectedId} onClose={closeDetail} /> : null}
    </section>
  );
}

function RangeForm({ from, to, onApply }: { from?: string; to?: string; onApply: (range: { from?: string; to?: string }) => void }) {
  const [draftFrom, setDraftFrom] = useState(from ?? "");
  const [draftTo, setDraftTo] = useState(to ?? "");
  const [error, setError] = useState<string>();

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if ((draftFrom && !parseJalaliDate(draftFrom)) || (draftTo && !parseJalaliDate(draftTo))) {
      setError("تاریخ شمسی معتبر وارد کنید؛ مثلاً ۱۴۰۵/۰۷/۰۱.");
      return;
    }
    setError(undefined);
    onApply({ from: draftFrom || undefined, to: draftTo || undefined });
  }

  return (
    <form className="flex flex-wrap items-end gap-3 rounded-xl border border-[var(--border)] bg-white p-4" onSubmit={submit}>
      {error ? <div className="w-full"><Alert>{error}</Alert></div> : null}
      <label className="w-40 space-y-1 text-sm font-bold">
        <span>از تاریخ</span>
        <Input value={draftFrom} onChange={(event) => setDraftFrom(event.target.value.trim())} placeholder="۱۴۰۵/۰۷/۰۱" inputMode="numeric" />
      </label>
      <label className="w-40 space-y-1 text-sm font-bold">
        <span>تا تاریخ</span>
        <Input value={draftTo} onChange={(event) => setDraftTo(event.target.value.trim())} placeholder="۱۴۰۵/۰۷/۰۱" inputMode="numeric" />
      </label>
      <Button type="submit">اعمال بازه</Button>
      <Button type="button" variant="secondary" onClick={() => onApply({ from: jalaliDaysAgo(6), to: todayJalali() })}>۷ روز اخیر</Button>
      <Button type="button" variant="secondary" onClick={() => onApply({ from: jalaliDaysAgo(29), to: todayJalali() })}>۳۰ روز اخیر</Button>
      <Button type="button" variant="ghost" onClick={() => onApply({})}>کل دوره</Button>
    </form>
  );
}

function ActivityCards({ countsByAction }: { countsByAction: { action: string; outcome: string; count: number | string }[] }) {
  const summary = summarizeActivity(countsByAction);
  const cards = [
    ...summary.categories.map((category) => ({ key: category.key, label: category.label, count: category.count })),
    { key: "denied", label: "تلاش ردشده (بدون دسترسی)", count: summary.denied },
    { key: "failed", label: "ناموفق", count: summary.failed },
    ...(summary.other > 0 ? [{ key: "other", label: "سایر", count: summary.other }] : []),
  ];

  return (
    <div className="space-y-3">
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {cards.map((card) => (
          <div className="rounded-xl border border-[var(--border)] bg-white p-4 shadow-sm" key={card.key}>
            <p className="text-sm text-[var(--muted)]">{card.label}</p>
            <p className="mt-1 text-2xl font-black">{formatPersianNumber(card.count)}</p>
          </div>
        ))}
      </div>
      {countsByAction.length > 0 ? (
        <details className="rounded-xl border border-[var(--border)] bg-white p-4 text-sm">
          <summary className="cursor-pointer font-bold">ریز شمارش به تفکیک عملیات</summary>
          <ul className="mt-3 space-y-1">
            {countsByAction.map((item) => (
              <li className="flex justify-between gap-3" key={`${item.action}-${item.outcome}`}>
                <span>{actionLabel(item.action)} — {outcomeLabel(item.outcome)}</span>
                <span className="font-bold">{formatPersianNumber(item.count)}</span>
              </li>
            ))}
          </ul>
        </details>
      ) : null}
    </div>
  );
}
