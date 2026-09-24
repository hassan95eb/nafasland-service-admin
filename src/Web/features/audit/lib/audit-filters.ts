import { jalaliDayEndUtc, jalaliDayStartUtc, parseJalaliDate } from "../../../shared/lib/jalali-date.ts";
import { isAuditOutcome, type AuditOutcomeName } from "./audit-labels.ts";

// The global log's filters live in the URL query string (shareable,
// refresh-proof). Dates are kept there as the Persian dates the user typed
// ("1405-07-01") and only turned into UTC instants when calling the API (ADR-035).

export interface AuditFilters {
  actorUserId?: string;
  from?: string;
  to?: string;
  action?: string;
  entityType?: string;
  outcome?: AuditOutcomeName;
}

const filterKeys = ["actorUserId", "from", "to", "action", "entityType", "outcome"] as const;

export function parseAuditFilters(params: URLSearchParams): AuditFilters {
  const filters: AuditFilters = {};
  for (const key of filterKeys) {
    const value = params.get(key)?.trim();
    if (!value) continue;
    if (key === "outcome") {
      if (isAuditOutcome(value)) filters.outcome = value;
    } else if (key === "from" || key === "to") {
      if (parseJalaliDate(value)) filters[key] = value;
    } else {
      filters[key] = value;
    }
  }
  return filters;
}

export function serializeAuditFilters(filters: AuditFilters) {
  const params = new URLSearchParams();
  for (const key of filterKeys) {
    const value = filters[key];
    if (value) params.set(key, value);
  }
  return params;
}

/** Query parameters for GET /audit/logs and the body of POST /audit/export — exactly the active filters, nothing else. */
export function toApiFilters(filters: AuditFilters) {
  return {
    actorUserId: filters.actorUserId ?? null,
    from: filters.from ? jalaliDayStartUtc(filters.from) ?? null : null,
    to: filters.to ? jalaliDayEndUtc(filters.to) ?? null : null,
    action: filters.action ?? null,
    entityType: filters.entityType ?? null,
    outcome: filters.outcome ?? null,
  };
}
