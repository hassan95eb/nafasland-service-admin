import { normalizeNumericInput } from "../../../shared/lib/normalize-number.ts";
import { isAuditOutcome, type AuditOutcomeName } from "./audit-labels.ts";
import { jalaliMonthLength, toGregorian, toJalali } from "./jalali.ts";

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

export interface JalaliDate {
  jy: number;
  jm: number;
  jd: number;
}

/** Accepts "۱۴۰۵/۷/۱", "1405-07-01" and mixed digits; null when it is not a real Jalali date. */
export function parseJalaliDate(input: string): JalaliDate | null {
  const normalized = String(normalizeNumericInput(input));
  const match = /^(\d{4})[/-](\d{1,2})[/-](\d{1,2})$/.exec(normalized);
  if (!match) return null;
  const [jy, jm, jd] = match.slice(1).map(Number);
  if (jm < 1 || jm > 12 || jd < 1 || jd > jalaliMonthLength(jy, jm)) return null;
  return { jy, jm, jd };
}

export function formatJalaliDate({ jy, jm, jd }: JalaliDate) {
  return `${jy}-${String(jm).padStart(2, "0")}-${String(jd).padStart(2, "0")}`;
}

// Iran has had no daylight saving since 2022, so Tehran time is a fixed UTC+03:30.
const tehranOffsetMinutes = 210;

function tehranMidnightUtc(date: JalaliDate) {
  const { gy, gm, gd } = toGregorian(date.jy, date.jm, date.jd);
  return Date.UTC(gy, gm - 1, gd) - tehranOffsetMinutes * 60_000;
}

/** Start of that Tehran day, as a UTC ISO string. */
export function jalaliDayStartUtc(input: string) {
  const date = parseJalaliDate(input);
  return date ? new Date(tehranMidnightUtc(date)).toISOString() : undefined;
}

/** Last millisecond of that Tehran day, as a UTC ISO string — "to" is inclusive of the whole day. */
export function jalaliDayEndUtc(input: string) {
  const date = parseJalaliDate(input);
  return date ? new Date(tehranMidnightUtc(date) + 86_400_000 - 1).toISOString() : undefined;
}

export function todayJalali(now = new Date()) {
  const tehran = new Date(now.getTime() + tehranOffsetMinutes * 60_000);
  return formatJalaliDate(toJalali(tehran.getUTCFullYear(), tehran.getUTCMonth() + 1, tehran.getUTCDate()));
}

export function jalaliDaysAgo(days: number, now = new Date()) {
  return todayJalali(new Date(now.getTime() - days * 86_400_000));
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
