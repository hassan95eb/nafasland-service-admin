"use client";

import { useState, type FormEvent } from "react";

import { useAuditActors } from "@/features/audit/hooks/use-audit";
import { parseJalaliDate, type AuditFilters } from "@/features/audit/lib/audit-filters";
import { actionLabels, actorDisplayName, entityTypeLabels, outcomeLabels, outcomeNames } from "@/features/audit/lib/audit-labels";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";

const selectClassName = "min-h-11 w-full rounded-lg border border-[var(--border)] bg-white px-3 text-sm";

/** Edits a draft locally; only "apply" writes it to the URL, so typing a date does not refetch on every keystroke. */
export function AuditFiltersForm({ value, onApply }: { value: AuditFilters; onApply: (filters: AuditFilters) => void }) {
  const actors = useAuditActors();
  const [draft, setDraft] = useState<AuditFilters>(value);
  const [error, setError] = useState<string>();

  function update<K extends keyof AuditFilters>(key: K, next: AuditFilters[K]) {
    setDraft((current) => ({ ...current, [key]: next || undefined }));
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    for (const [key, label] of [["from", "از تاریخ"], ["to", "تا تاریخ"]] as const) {
      const date = draft[key];
      if (date && !parseJalaliDate(date)) {
        setError(`«${label}» یک تاریخ شمسی معتبر نیست؛ مثلاً ۱۴۰۵/۰۷/۰۱.`);
        return;
      }
    }
    setError(undefined);
    onApply(draft);
  }

  function reset() {
    setDraft({});
    setError(undefined);
    onApply({});
  }

  return (
    <form className="grid gap-3 rounded-xl border border-[var(--border)] bg-white p-4 sm:grid-cols-2 lg:grid-cols-3" onSubmit={submit}>
      {error ? <div className="sm:col-span-2 lg:col-span-3"><Alert>{error}</Alert></div> : null}

      <label className="space-y-1 text-sm font-bold">
        <span>کاربر</span>
        <select className={selectClassName} value={draft.actorUserId ?? ""} onChange={(event) => update("actorUserId", event.target.value)}>
          <option value="">همه</option>
          {(actors.data ?? []).map((actor) => (
            <option key={actor.userId} value={actor.userId}>
              {actorDisplayName({ actorUserId: actor.userId, actorUsername: actor.username })}
            </option>
          ))}
          {draft.actorUserId && !actors.data?.some((actor) => actor.userId === draft.actorUserId) ? (
            <option value={draft.actorUserId}>{actorDisplayName({ actorUserId: draft.actorUserId, actorUsername: null })}</option>
          ) : null}
        </select>
      </label>

      <label className="space-y-1 text-sm font-bold">
        <span>از تاریخ</span>
        <Input value={draft.from ?? ""} onChange={(event) => update("from", event.target.value.trim())} placeholder="۱۴۰۵/۰۷/۰۱" inputMode="numeric" />
      </label>

      <label className="space-y-1 text-sm font-bold">
        <span>تا تاریخ</span>
        <Input value={draft.to ?? ""} onChange={(event) => update("to", event.target.value.trim())} placeholder="۱۴۰۵/۰۷/۰۱" inputMode="numeric" />
      </label>

      <label className="space-y-1 text-sm font-bold">
        <span>عملیات</span>
        <select className={selectClassName} value={draft.action ?? ""} onChange={(event) => update("action", event.target.value)}>
          <option value="">همه</option>
          {Object.entries(actionLabels).map(([action, label]) => (
            <option key={action} value={action}>{label}</option>
          ))}
        </select>
      </label>

      <label className="space-y-1 text-sm font-bold">
        <span>نوع موجودیت</span>
        <select className={selectClassName} value={draft.entityType ?? ""} onChange={(event) => update("entityType", event.target.value)}>
          <option value="">همه</option>
          {Object.entries(entityTypeLabels).map(([entityType, label]) => (
            <option key={entityType} value={entityType}>{label}</option>
          ))}
        </select>
      </label>

      <label className="space-y-1 text-sm font-bold">
        <span>نتیجه</span>
        <select
          className={selectClassName}
          value={draft.outcome ?? ""}
          onChange={(event) => update("outcome", (event.target.value || undefined) as AuditFilters["outcome"])}
        >
          <option value="">همه</option>
          {outcomeNames.map((outcome) => (
            <option key={outcome} value={outcome}>{outcomeLabels[outcome]}</option>
          ))}
        </select>
      </label>

      <div className="flex flex-wrap gap-2 sm:col-span-2 lg:col-span-3">
        <Button type="submit">اعمال فیلتر</Button>
        <Button type="button" variant="secondary" onClick={reset}>حذف همهٔ فیلترها</Button>
      </div>
    </form>
  );
}
