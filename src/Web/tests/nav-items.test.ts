import assert from "node:assert/strict";
import test from "node:test";

import { filterNavItems } from "../shared/permissions/nav-items.ts";

test("فقط لینک‌هایی را نگه می‌دارد که permission آن‌ها وجود دارد", () => {
  assert.deepEqual(filterNavItems(["catalog.products.read"]).map((item) => item.href), ["/products"]);
  assert.deepEqual(filterNavItems(["audit.read.all"]), []);
});
