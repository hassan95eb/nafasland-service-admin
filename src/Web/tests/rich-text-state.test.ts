import assert from "node:assert/strict";
import test from "node:test";

import { changeRichText, createRichTextState, switchRichTextMode } from "../features/products/lib/rich-text-state.ts";

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
