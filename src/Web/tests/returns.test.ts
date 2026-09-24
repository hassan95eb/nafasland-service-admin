import assert from "node:assert/strict";
import test from "node:test";

import { lineTotal, orderStatusLabel } from "../features/returns/lib/order-labels.ts";
import { orderIdSchema, returnRequestSchema } from "../features/returns/schemas/return-request-schema.ts";

// 2026-09-25 11:30 Tehran = 1405-07-03
const now = new Date("2026-09-25T08:00:00Z");

test("وضعیت‌های شناخته‌شدهٔ سفارش برچسب فارسی و وضعیت ناشناخته نام خام می‌گیرد", () => {
  assert.equal(orderStatusLabel("paid"), "پرداخت‌شده");
  assert.equal(orderStatusLabel("fulfilled"), "تکمیل‌شده");
  assert.equal(orderStatusLabel("canceled"), "لغوشده");
  assert.equal(orderStatusLabel("on_hold"), "on_hold");
});

test("جمع ردیف قیمت واحد ضرب در تعداد است و بدون قیمت، خالی", () => {
  assert.equal(lineTotal(700000, 2), 1400000);
  assert.equal(lineTotal(null, 2), null);
});

test("شمارهٔ سفارش با ارقام فارسی نرمال می‌شود و فقط عدد صحیح مثبت پذیرفته است", () => {
  assert.equal(orderIdSchema.parse("۹۰۰۰۰۰۰۰۱"), "900000001");
  assert.equal(orderIdSchema.safeParse("0").success, false);
  assert.equal(orderIdSchema.safeParse("12.5").success, false);
  assert.equal(orderIdSchema.safeParse("abc").success, false);
});

test("فرم معتبر به payload با تاریخ میلادی ISO تبدیل می‌شود", () => {
  const result = returnRequestSchema({ now }).parse({ orderId: "900000001", reason: "  آسیب‌دیده  ", returnDate: "۱۴۰۵/۰۷/۰۲" });
  assert.deepEqual(result, { orderId: "900000001", reason: "آسیب‌دیده", returnDate: "2026-09-24" });
});

test("علت خالی رد می‌شود", () => {
  const result = returnRequestSchema({ now }).safeParse({ orderId: "1", reason: "   ", returnDate: "1405/07/02" });
  assert.equal(result.success, false);
  assert.deepEqual(result.error?.issues.map((issue) => issue.path[0]), ["reason"]);
});

test("تاریخ آینده و تاریخ پیش از ثبت سفارش رد می‌شوند", () => {
  const future = returnRequestSchema({ now }).safeParse({ orderId: "1", reason: "x", returnDate: "1405/07/04" });
  assert.equal(future.success, false);
  assert.equal(future.error?.issues[0].message, "تاریخ عودت نمی‌تواند در آینده باشد.");

  const beforeOrder = returnRequestSchema({ now, orderCreatedAtUtc: "2026-09-24T06:36:59Z" })
    .safeParse({ orderId: "1", reason: "x", returnDate: "1405/07/01" });
  assert.equal(beforeOrder.success, false);
  assert.equal(returnRequestSchema({ now }).safeParse({ orderId: "1", reason: "x", returnDate: "1405/07/03" }).success, true);
});
