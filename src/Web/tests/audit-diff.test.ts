import assert from "node:assert/strict";
import test from "node:test";

import { computeAuditDiff } from "../features/audit/lib/audit-diff.ts";

test("فقط فیلدهای ChangedFields را قبل ← بعد برمی‌گرداند", () => {
  const rows = computeAuditDiff(
    { title: "قدیم", price: 100, slug: "same" },
    { title: "جدید", price: 120, slug: "same" },
    ["title", "price"],
  );

  assert.deepEqual(rows, [
    { field: "title", before: { kind: "scalar", text: "قدیم" }, after: { kind: "scalar", text: "جدید" }, kind: "changed" },
    { field: "price", before: { kind: "scalar", text: "100" }, after: { kind: "scalar", text: "120" }, kind: "changed" },
  ]);
});

test("null، نبود کلید و رشتهٔ خالی همه «خالی» هستند", () => {
  const rows = computeAuditDiff({ effect: null, caption: "" }, { effect: "Grant", redirect: "/x" }, ["effect", "caption", "redirect"]);

  assert.deepEqual(rows.map((row) => [row.before.kind, row.after.kind]), [
    ["empty", "scalar"],
    ["empty", "empty"],
    ["empty", "scalar"],
  ]);
});

test("شیء و آرایهٔ تودرتو خلاصهٔ خوانا دارند، نه JSON خام", () => {
  const [categories, meta, empty] = computeAuditDiff(
    { categories: [{ id: "1" }], meta: { a: 1 }, roleIds: ["r1"] },
    { categories: [{ id: "1" }, { id: "2" }, { id: "3" }], meta: { a: 1, b: 2 }, roleIds: [] },
    ["categories", "meta", "roleIds"],
  );

  assert.equal(categories.before.kind === "collection" && categories.before.summary, "۱ مورد");
  assert.equal(categories.after.kind === "collection" && categories.after.summary, "۳ مورد");
  assert.equal(meta.after.kind === "collection" && meta.after.summary, "۲ فیلد");
  assert.ok(categories.after.kind === "collection" && categories.after.detail.includes('"id": "3"'));
  assert.equal(empty.after.kind, "empty");
});

test("توضیحات HTML متن خام است و بولی فارسی نمایش داده می‌شود", () => {
  const [description, isActive] = computeAuditDiff(
    { description: "<p>قدیم</p>", IsActive: true },
    { description: "<p onclick='x()'>جدید</p>", IsActive: false },
    ["description", "IsActive"],
  );

  assert.deepEqual(description.after, { kind: "html", text: "<p onclick='x()'>جدید</p>" });
  assert.deepEqual(isActive.before, { kind: "scalar", text: "بله" });
  assert.deepEqual(isActive.after, { kind: "scalar", text: "خیر" });
});

test("رکورد ایجاد بدون Before همهٔ فیلدهای After را «مقدار جدید» نشان می‌دهد", () => {
  const rows = computeAuditDiff(null, { Username: "ali", IsActive: true, Roles: [] }, null);

  assert.deepEqual(rows, [
    { field: "Username", before: { kind: "empty" }, after: { kind: "scalar", text: "ali" }, kind: "created" },
    { field: "IsActive", before: { kind: "empty" }, after: { kind: "scalar", text: "بله" }, kind: "created" },
    { field: "Roles", before: { kind: "empty" }, after: { kind: "empty" }, kind: "created" },
  ]);
});

test("رکورد بدون Before/After (مثلاً Denied) هیچ ردیفی ندارد", () => {
  assert.deepEqual(computeAuditDiff(null, null, null), []);
  assert.deepEqual(computeAuditDiff({ a: 1 }, { a: 1 }, []), []);
});
