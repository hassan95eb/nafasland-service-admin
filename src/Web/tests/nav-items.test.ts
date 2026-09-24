import assert from "node:assert/strict";
import test from "node:test";

import { filterNavItems } from "../shared/permissions/nav-items.ts";

test("فقط لینک‌هایی را نگه می‌دارد که permission آن‌ها وجود دارد", () => {
  assert.deepEqual(filterNavItems(["catalog.products.read"]).map((item) => item.href), ["/products", "/approvals"]);
  assert.deepEqual(filterNavItems(["identity.users.manage"]).map((item) => item.href), ["/admins", "/approvals"]);
  assert.deepEqual(
    filterNavItems(["catalog.products.read", "identity.users.manage"]).map((item) => item.href),
    ["/products", "/admins", "/approvals"],
  );
});

test("«گزارش فعالیت» فقط برای audit.read.all نمایش داده می‌شود (ADR-014)", () => {
  assert.deepEqual(filterNavItems(["audit.read.all"]).map((item) => item.href), ["/approvals", "/admin/audit"]);
  assert.deepEqual(filterNavItems(["audit.export"]).map((item) => item.href), ["/approvals"]);
});

test("«درخواست‌های من» بدون هیچ permission ای برای هر کاربر واردشده نمایش داده می‌شود", () => {
  assert.deepEqual(filterNavItems([]).map((item) => item.href), ["/approvals"]);
});

test("«کارتابل تأیید» فقط برای approvals.read.all نمایش داده می‌شود", () => {
  assert.deepEqual(filterNavItems(["approvals.read.all"]).map((item) => item.href), ["/approvals", "/approvals/review"]);
});
