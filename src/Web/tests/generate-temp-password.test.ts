import assert from "node:assert/strict";
import test from "node:test";

import { generateTemporaryPassword, temporaryPasswordLength } from "../features/admins/lib/generate-temp-password.ts";

test("رمز موقت طول کافی و ترکیب حروف و عدد دارد", () => {
  for (let attempt = 0; attempt < 100; attempt++) {
    const password = generateTemporaryPassword();
    assert.equal(password.length, temporaryPasswordLength);
    assert.match(password, /[a-z]/);
    assert.match(password, /[A-Z]/);
    assert.match(password, /[0-9]/);
  }
});

test("هر بار رمز تازه‌ای تولید می‌شود", () => {
  const generated = new Set(Array.from({ length: 20 }, generateTemporaryPassword));
  assert.equal(generated.size, 20);
});
