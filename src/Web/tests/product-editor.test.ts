import assert from "node:assert/strict";
import test from "node:test";

import { buildVariantTitles, completeAttributes, createVariantSchema, isSelectableCategory, parseAttributeValues, selectedFilterIds, validateProductKind } from "../features/products/schemas/product-editor-schema.ts";
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

test("مقدار ویژگی با + (و ویرگول) جدا و تکراری‌ها حذف می‌شوند", () => {
  assert.deepEqual(parseAttributeValues(" زرد + آبی ، زرد,سبز "), ["زرد", "آبی", "سبز"]);
  assert.deepEqual(completeAttributes([{ name: "رنگ", value: "زرد" }, { name: "", value: "x" }, { name: "اندازه", value: " + " }]), [
    { name: "رنگ", values: ["زرد"] },
  ]);
});

test("عنوان واریانت‌ها ضرب دکارتی ویژگی‌ها به قالب پنل نفس‌لند است", () => {
  assert.deepEqual(buildVariantTitles([]), []);
  assert.deepEqual(buildVariantTitles([{ name: "رنگ", values: ["زرد", "آبی"] }]), ["رنگ: زرد", "رنگ: آبی"]);
  assert.deepEqual(
    buildVariantTitles([{ name: "رنگ", values: ["زرد", "آبی"] }, { name: "اندازه", values: ["کوچک", "بزرگ"] }]),
    ["رنگ: زرد، اندازه: کوچک", "رنگ: زرد، اندازه: بزرگ", "رنگ: آبی، اندازه: کوچک", "رنگ: آبی، اندازه: بزرگ"],
  );
});

test("موجودی و حداقل سفارش عدد صحیح‌اند و قیمت ارقام فارسی را می‌پذیرد", () => {
  const base = { title: "رنگ: زرد", price: "۱۲٬۰۰۰", comparePrice: "", stock: "5", minimum: "", maximum: "", weight: "", length: "", width: "", height: "", sku: "" };
  const parsed = createVariantSchema.parse(base);
  assert.equal(parsed.price, 12000);
  assert.equal(parsed.comparePrice, null);
  assert.equal(createVariantSchema.safeParse({ ...base, stock: "1.5" }).success, false);
});
