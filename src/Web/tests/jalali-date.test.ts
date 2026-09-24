import assert from "node:assert/strict";
import test from "node:test";

import {
  jalaliDayEndUtc,
  jalaliDayStartUtc,
  jalaliToIsoDate,
  parseJalaliDate,
  todayJalali,
} from "../shared/lib/jalali-date.ts";
import { toGregorian, toJalali } from "../shared/lib/jalali.ts";

test("تبدیل شمسی ↔ میلادی با نمونهٔ ADR-035 می‌خواند", () => {
  assert.deepEqual(toGregorian(1405, 6, 22), { gy: 2026, gm: 9, gd: 13 });
  assert.deepEqual(toJalali(2026, 9, 13), { jy: 1405, jm: 6, jd: 22 });
  assert.deepEqual(toGregorian(1403, 12, 30), { gy: 2025, gm: 3, gd: 20 });
});

test("تاریخ شمسی با ارقام فارسی و هر دو جداکننده پذیرفته می‌شود و تاریخ ناموجود رد", () => {
  assert.deepEqual(parseJalaliDate("۱۴۰۵/۷/۱"), { jy: 1405, jm: 7, jd: 1 });
  assert.deepEqual(parseJalaliDate("1405-07-01"), { jy: 1405, jm: 7, jd: 1 });
  assert.equal(parseJalaliDate("1405/07/31"), null);
  assert.equal(parseJalaliDate("1404/12/30"), null);
  assert.equal(parseJalaliDate("دیروز"), null);
});

test("بازهٔ روز شمسی به UTC با ساعت تهران تبدیل می‌شود و «تا» کل روز را شامل است", () => {
  assert.equal(jalaliDayStartUtc("1405/06/22"), "2026-09-12T20:30:00.000Z");
  assert.equal(jalaliDayEndUtc("1405/06/22"), "2026-09-13T20:29:59.999Z");
  assert.equal(todayJalali(new Date("2026-09-12T21:00:00Z")), "1405-06-22");
});

test("روز شمسی به تاریخ میلادی ISO تبدیل می‌شود", () => {
  assert.equal(jalaliToIsoDate("۱۴۰۵/۷/۲"), "2026-09-24");
  assert.equal(jalaliToIsoDate("1405/07/31"), undefined);
});
