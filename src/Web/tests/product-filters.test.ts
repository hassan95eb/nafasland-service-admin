import assert from "node:assert/strict";
import test from "node:test";

import { filterProductsWithoutPrice } from "../features/products/schemas/product-filters.ts";

const products = [
  { id: "a", price: null },
  { id: "b", price: 0 },
  { id: "c", price: 120000 },
  { id: "d" },
];

test("فیلتر بدون قیمت فقط مقدار صریح null را نگه می‌دارد و صفر را قیمت می‌داند", () => {
  assert.deepEqual(filterProductsWithoutPrice(products, true), [{ id: "a", price: null }]);
});

test("فیلتر خاموش همهٔ اقلام صفحه را نگه می‌دارد", () => {
  assert.deepEqual(filterProductsWithoutPrice(products, false), products);
});
