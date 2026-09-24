import assert from "node:assert/strict";
import test from "node:test";

import { changeRichText, createRichTextState, normalizeEditorUrl, switchRichTextMode } from "../features/products/lib/rich-text-state.ts";

test("بازکردن و جابه‌جایی نمای HTML dirty ایجاد نمی‌کند", () => {
  const initial = createRichTextState("<p>اصل&nbsp;</p>");
  const html = switchRichTextMode(initial, "html");
  const visual = switchRichTextMode(html, "visual");

  assert.equal(visual.dirty, false);
  assert.equal(visual.value, "<p>اصل&nbsp;</p>");
  assert.equal(visual.original, visual.value);
});

test("dirty tracking هر فیلد مستقل است و تغییر mode داده را از دست نمی‌دهد", () => {
  const first = changeRichText(createRichTextState("اول"), "اول جدید");
  const second = createRichTextState("دوم");
  const switched = switchRichTextMode(first, "html");

  assert.equal(switched.value, "اول جدید");
  assert.equal(switched.dirty, true);
  assert.equal(second.value, "دوم");
  assert.equal(second.dirty, false);
});

test("نشانی لینک و تصویر فقط http(s) یا مسیر نسبی سایت است", () => {
  assert.equal(normalizeEditorUrl(" https://nafasland.com/a?b=1 "), "https://nafasland.com/a?b=1");
  assert.equal(normalizeEditorUrl("nafasland.com/shop"), "https://nafasland.com/shop");
  assert.equal(normalizeEditorUrl("/uploads/products/1.jpg"), "/uploads/products/1.jpg");
  assert.equal(normalizeEditorUrl("javascript:alert(1)"), undefined);
  assert.equal(normalizeEditorUrl("data:text/html,x"), undefined);
  assert.equal(normalizeEditorUrl("//evil.example"), undefined);
  assert.equal(normalizeEditorUrl("two words"), undefined);
  assert.equal(normalizeEditorUrl(""), undefined);
});
