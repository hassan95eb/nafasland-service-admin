import assert from "node:assert/strict";
import test from "node:test";

import { createIdempotencyKey } from "../features/products/api/update-variant.ts";
import { updateVariantSchema } from "../features/products/schemas/update-variant-schema.ts";
import { normalizeNumericInput } from "../shared/lib/normalize-number.ts";

test("ارقام فارسی و عربی و جداکنندهٔ هزارگان در یک نقطه نرمال می‌شوند", () => {
  assert.equal(normalizeNumericInput("۱٬۲۳۴٬۵۶۷"), "1234567");
  assert.equal(normalizeNumericInput("١,٢٣٤"), "1234");
});

test("schema قیمت و موجودی مقدار نرمال‌شده را اعتبارسنجی می‌کند", () => {
  assert.deepEqual(updateVariantSchema.parse({ newPrice: "۱۲۵٬۰۰۰", newStock: "٧" }), {
    newPrice: 125000,
    newStock: 7,
  });
});

test("کلید idempotency فرم فقط از مولد UUID داده‌شده ساخته می‌شود", () => {
  assert.equal(createIdempotencyKey(() => "00000000-0000-4000-8000-000000000001"), "00000000-0000-4000-8000-000000000001");
});
