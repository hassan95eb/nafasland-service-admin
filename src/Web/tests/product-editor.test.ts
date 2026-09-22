import assert from "node:assert/strict";
import test from "node:test";

import { isSelectableCategory, selectedFilterIds, validateProductKind } from "../features/products/schemas/product-editor-schema.ts";
import { sanitizeAllowedStyle } from "../shared/lib/sanitize-html.ts";

test("اعتبارسنجی کالای ساده فقط primary یکتا را می‌پذیرد", () => {
  assert.equal(validateProductKind("simple", ["primary"]), undefined);
  assert.ok(validateProductKind("simple", ["primary", "دوم"]));
  assert.ok(validateProductKind("simple", ["رنگ: قرمز"]));
});

test("اعتبارسنجی کالای چندواریانتی عنوان صریح می‌خواهد", () => {
  assert.equal(validateProductKind("variable", ["رنگ: قرمز", "رنگ: آبی"]), undefined);
  assert.ok(validateProductKind("variable", []));
  assert.ok(validateProductKind("variable", [""]));
});

test("فقط صفحه‌های store انتخاب‌پذیر و فقط شناسهٔ برگ فیلتر ارسال می‌شود", () => {
  assert.equal(isSelectableCategory("store"), true);
  assert.equal(isSelectableCategory("blog"), false);
  assert.deepEqual(selectedFilterIds(new Set(["178849396", "178865233"])), [178849396, 178865233]);
});

test("allowlist استایل فقط text-align و font-weight را نگه می‌دارد", () => {
  assert.equal(
    sanitizeAllowedStyle("text-align: justify; color: red; font-weight: 700"),
    "text-align: justify; font-weight: 700",
  );
});
