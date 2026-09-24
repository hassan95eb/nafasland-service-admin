import assert from "node:assert/strict";
import test from "node:test";

import { actionLabel, actorDisplayName, entityTypeLabel, outcomeLabel, summarizeActivity } from "../features/audit/lib/audit-labels.ts";

test("Actionهای شناخته‌شده برچسب فارسی دارند", () => {
  assert.equal(actionLabel("ProductUpdated"), "ویرایش محصول");
  assert.equal(actionLabel("ApprovalExpired"), "انقضای درخواست");
  assert.equal(actionLabel("AuditPurged"), "پاک‌سازی دوره‌ای گزارش فعالیت");
});

test("Action ناشناخته با همان نام خام نمایش داده می‌شود، نه خطا", () => {
  assert.equal(actionLabel("InventorySynced"), "InventorySynced");
  assert.equal(actionLabel("toString"), "toString");
  assert.equal(entityTypeLabel("Warehouse"), "Warehouse");
  assert.equal(entityTypeLabel(null), "—");
  assert.equal(outcomeLabel("Partial"), "Partial");
});

test("عامل: سیستم، کاربر ناشناس با شناسهٔ کوتاه، یا نام کاربری", () => {
  assert.equal(actorDisplayName({ actorUserId: null, actorUsername: null }), "سیستم");
  assert.equal(
    actorDisplayName({ actorUserId: "3f2a9c1e-0000-4000-8000-000000000000", actorUsername: null }),
    "کاربر ناشناس (3f2a9c1e)",
  );
  assert.equal(actorDisplayName({ actorUserId: "3f2a9c1e-0000-4000-8000-000000000000", actorUsername: "ali" }), "ali");
});

test("کارت‌های فعالیت فقط کار موفق را می‌شمارند و Denied/Failed را جدا", () => {
  const summary = summarizeActivity([
    { action: "ProductCreated", outcome: "Success", count: 2 },
    { action: "ProductUpdated", outcome: "Success", count: 3 },
    { action: "VariantPriceInventoryUpdated", outcome: "Success", count: "1" },
    { action: "ProductUpdated", outcome: "Denied", count: 4 },
    { action: "ApprovalRequested", outcome: "Success", count: 1 },
    { action: "ApprovalRequested", outcome: "Failed", count: 1 },
    { action: "InventorySynced", outcome: "Success", count: 5 },
  ]);

  assert.deepEqual(
    Object.fromEntries(summary.categories.map((category) => [category.key, category.count])),
    { create: 2, edit: 4, request: 1, decision: 0 },
  );
  assert.equal(summary.denied, 4);
  assert.equal(summary.failed, 1);
  assert.equal(summary.other, 5);
});
