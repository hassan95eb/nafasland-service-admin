import { normalizeNumericInput } from "./normalize-number.ts";
import { jalaliMonthLength, toGregorian, toJalali } from "./jalali.ts";

// Persian (Jalali) date strings as the user types them ("1405-07-01"), and
// their conversion to what the API takes: UTC instants for ranges, plain
// Gregorian dates for a calendar day (ADR-035). Shared by the activity report
// filters and the return form.

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

/** A Jalali day as an ISO calendar date ("2026-09-24"), for an API field that is a date, not an instant. */
export function jalaliToIsoDate(input: string) {
  const date = parseJalaliDate(input);
  if (!date) return undefined;
  const { gy, gm, gd } = toGregorian(date.jy, date.jm, date.jd);
  return `${gy}-${String(gm).padStart(2, "0")}-${String(gd).padStart(2, "0")}`;
}
