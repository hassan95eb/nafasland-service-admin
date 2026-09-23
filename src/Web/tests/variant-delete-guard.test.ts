import assert from "node:assert/strict";
import test from "node:test";

import { isLastVariant } from "../features/products/lib/variant-delete-guard.ts";

test("تنها یک واریانت باقی‌مانده، آخرین واریانت است", () => {
  assert.equal(isLastVariant(1), true);
});

test("صفر واریانت هم به‌عنوان حالت مرزی آخرین در نظر گرفته می‌شود", () => {
  assert.equal(isLastVariant(0), true);
});

test("بیش از یک واریانت آخرین واریانت نیست", () => {
  assert.equal(isLastVariant(2), false);
  assert.equal(isLastVariant(5), false);
});
