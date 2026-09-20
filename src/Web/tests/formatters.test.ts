import assert from "node:assert/strict";
import test from "node:test";

import { formatPersianDate, formatPrice } from "../shared/lib/formatters.ts";

test("قیمت null را با پیام صریح نمایش می‌دهد", () => {
  assert.equal(formatPrice(null), "قیمت تعریف نشده");
});

test("قیمت را با جداکننده، ارقام فارسی و واحد نمایش می‌دهد", () => {
  assert.equal(formatPrice(123456), "۱۲۳٬۴۵۶ تومان");
});

test("تاریخ UTC را در تقویم شمسی نمایش می‌دهد", () => {
  assert.equal(formatPersianDate("2026-03-21T00:00:00Z"), "۱۴۰۵/۰۱/۰۱");
});
