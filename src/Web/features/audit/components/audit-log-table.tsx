import type { AuditLogSummary } from "@/features/audit/api/audit-api";
import { ActorLink } from "@/features/audit/components/actor-link";
import { AuditOutcomeBadge } from "@/features/audit/components/audit-outcome-badge";
import { actionLabel, entityTypeLabel } from "@/features/audit/lib/audit-labels";
import { formatPersianDateTime } from "@/shared/lib/formatters";

export function AuditLogTable({
  items,
  onSelect,
  selectedId,
  showActor = true,
}: {
  items: AuditLogSummary[];
  onSelect: (id: string) => void;
  selectedId: string | null;
  showActor?: boolean;
}) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[860px] border-collapse text-right text-sm">
        <thead className="bg-neutral-50 text-xs text-[var(--muted)]">
          <tr>
            <th className="px-4 py-3 font-bold">زمان</th>
            {showActor ? <th className="px-4 py-3 font-bold">عامل</th> : null}
            <th className="px-4 py-3 font-bold">نقش در آن زمان</th>
            <th className="px-4 py-3 font-bold">عملیات</th>
            <th className="px-4 py-3 font-bold">موجودیت</th>
            <th className="px-4 py-3 font-bold">نتیجه</th>
            <th className="px-4 py-3 font-bold">دلیل شکست</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr
              key={item.id}
              className={`cursor-pointer border-t border-[var(--border)] hover:bg-emerald-50/60 ${selectedId === item.id ? "bg-emerald-50" : ""}`}
              onClick={() => onSelect(item.id)}
            >
              <td className="whitespace-nowrap px-4 py-3">
                <button
                  type="button"
                  className="text-right hover:underline"
                  onClick={(event) => {
                    event.stopPropagation();
                    onSelect(item.id);
                  }}
                >
                  {formatPersianDateTime(item.createdAt)}
                </button>
              </td>
              {showActor ? (
                <td className="px-4 py-3">
                  <ActorLink actor={item} />
                </td>
              ) : null}
              <td className="px-4 py-3 text-xs text-[var(--muted)]">{item.actorRoleAtTime}</td>
              <td className="px-4 py-3 font-bold">{actionLabel(item.action)}</td>
              <td className="px-4 py-3">
                {entityTypeLabel(item.entityType)}
                {item.entityId ? <span className="block text-xs text-[var(--muted)]" dir="ltr">{item.entityId}</span> : null}
              </td>
              <td className="px-4 py-3">
                <AuditOutcomeBadge outcome={item.outcome} />
              </td>
              <td className="max-w-xs px-4 py-3 text-xs leading-6 text-[var(--muted)]">{item.failureReason ?? "—"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
