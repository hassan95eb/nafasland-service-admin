"use client";

import { useEffect, useState, type ReactNode } from "react";

import { useAuditLog } from "@/features/audit/hooks/use-audit";
import { ActorLink } from "@/features/audit/components/actor-link";
import { AuditOutcomeBadge } from "@/features/audit/components/audit-outcome-badge";
import { computeAuditDiff, type DiffRow, type DiffValue } from "@/features/audit/lib/audit-diff";
import { actionLabel, entityTypeLabel } from "@/features/audit/lib/audit-labels";
import { presentApiError } from "@/shared/lib/api-client";
import { formatPersianDateTime } from "@/shared/lib/formatters";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";

/** ADR-014, view 3: one record's detail in a side panel, with a field-by-field "before → after" (never raw JSON). */
export function AuditEventDetail({ id, onClose }: { id: string; onClose: () => void }) {
  const query = useAuditLog(id);

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
        aria-label="جزئیات رویداد"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="mb-4 flex items-center justify-between gap-3">
          <h2 className="text-lg font-black">جزئیات رویداد</h2>
          <Button type="button" variant="ghost" onClick={onClose}>بستن</Button>
        </div>

        {query.isPending ? (
          <p className="py-12 text-center text-sm text-[var(--muted)]">در حال دریافت جزئیات رویداد…</p>
        ) : query.error || !query.data ? (
          <Alert>{presentApiError(query.error)}</Alert>
        ) : (
          <EventBody detail={query.data} />
        )}
      </aside>
    </div>
  );
}

function EventBody({ detail }: { detail: NonNullable<ReturnType<typeof useAuditLog>["data"]> }) {
  const rows = computeAuditDiff(detail.before, detail.after, detail.changedFields);

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-base font-black">{actionLabel(detail.action)}</span>
        <AuditOutcomeBadge outcome={detail.outcome} />
      </div>

      <dl className="grid gap-x-4 gap-y-3 text-sm sm:grid-cols-[10rem_minmax(0,1fr)]">
        <Field label="زمان">{formatPersianDateTime(detail.createdAt)}</Field>
        <Field label="عامل"><ActorLink actor={detail} /></Field>
        <Field label="نقش در آن زمان">{detail.actorRoleAtTime}</Field>
        {detail.onBehalfOfUserId ? (
          <Field label="به درخواست">
            <ActorLink actor={{ actorUserId: detail.onBehalfOfUserId, actorUsername: detail.onBehalfOfUsername }} />
          </Field>
        ) : null}
        <Field label="موجودیت">
          {entityTypeLabel(detail.entityType)} {detail.entityId ? <span dir="ltr">{detail.entityId}</span> : null}
        </Field>
        {detail.parentEntityId ? (
          <Field label="متعلق به">
            {entityTypeLabel(detail.parentEntityType)} <span dir="ltr">{detail.parentEntityId}</span>
          </Field>
        ) : null}
        {detail.failureReason ? <Field label="دلیل شکست">{detail.failureReason}</Field> : null}
        <Field label="کد پیگیری"><CopyableCode value={detail.correlationId} /></Field>
        {detail.ipAddress ? <Field label="نشانی IP"><span dir="ltr">{detail.ipAddress}</span></Field> : null}
        {detail.userAgent ? <Field label="مرورگر"><span className="break-all text-xs" dir="ltr">{detail.userAgent}</span></Field> : null}
      </dl>

      <section className="space-y-3">
        <h3 className="font-black">تغییرات</h3>
        {rows.length === 0 ? (
          <p className="rounded-lg bg-neutral-50 p-4 text-sm text-[var(--muted)]">
            برای این رویداد تغییری فیلدبه‌فیلد ثبت نشده است (مثلاً تلاش ردشده یا رویدادی بدون دادهٔ قبل و بعد).
          </p>
        ) : (
          <DiffTable rows={rows} />
        )}
      </section>
    </div>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <>
      <dt className="font-bold text-[var(--muted)]">{label}</dt>
      <dd className="min-w-0">{children}</dd>
    </>
  );
}

function CopyableCode({ value }: { value: string }) {
  const [copied, setCopied] = useState(false);

  async function copy() {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2_000);
    } catch {
      // Clipboard can be unavailable (non-secure context); the code stays selectable.
    }
  }

  return (
    <span className="flex flex-wrap items-center gap-2">
      <code className="select-all break-all rounded bg-neutral-100 px-2 py-1 text-xs" dir="ltr">{value}</code>
      <Button type="button" variant="secondary" className="min-h-8 px-3 py-1 text-xs" onClick={copy}>
        {copied ? "کپی شد" : "کپی"}
      </Button>
    </span>
  );
}

function DiffTable({ rows }: { rows: DiffRow[] }) {
  const onlyCreated = rows.every((row) => row.kind === "created");

  return (
    <div className="overflow-x-auto rounded-lg border border-[var(--border)]">
      <table className="w-full border-collapse text-right text-sm">
        <thead className="bg-neutral-50 text-xs text-[var(--muted)]">
          <tr>
            <th className="px-3 py-2 font-bold">فیلد</th>
            {onlyCreated ? null : <th className="px-3 py-2 font-bold">مقدار قبل</th>}
            <th className="px-3 py-2 font-bold">{onlyCreated ? "مقدار جدید" : "مقدار بعد"}</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr className="border-t border-[var(--border)] align-top" key={row.field}>
              <td className="px-3 py-2 font-bold" dir="ltr">{row.field}</td>
              {onlyCreated ? null : (
                <td className="px-3 py-2">
                  <DiffCell value={row.before} />
                </td>
              )}
              <td className="px-3 py-2">
                {onlyCreated ? null : <span className="ml-1 text-[var(--muted)]" aria-hidden>←</span>}
                <DiffCell value={row.after} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function DiffCell({ value }: { value: DiffValue }) {
  switch (value.kind) {
    case "empty":
      return <span className="text-[var(--muted)]">—</span>;
    case "scalar":
      return <span className="break-words">{value.text}</span>;
    case "html":
      // Product description HTML as source text, never rendered (React escapes it).
      return (
        <pre className="max-h-48 overflow-auto whitespace-pre-wrap break-words rounded bg-neutral-50 p-2 text-xs" dir="ltr">
          {value.text}
        </pre>
      );
    case "collection":
      return (
        <details>
          <summary className="cursor-pointer text-[var(--primary)]">{value.summary}</summary>
          <pre className="mt-2 max-h-64 overflow-auto rounded bg-neutral-50 p-2 text-xs" dir="ltr">{value.detail}</pre>
        </details>
      );
  }
}
